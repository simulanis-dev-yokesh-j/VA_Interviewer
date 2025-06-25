using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddHttpClient().AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.MaxDepth = HttpHelper.BotMessageSerializerSettings.MaxDepth;
});

// Create the Bot Framework Authentication to be used with the Bot Adapter.
builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

// Create the Bot Adapter with error handling enabled.
builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

// Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
builder.Services.AddTransient<IBot, InterviewBot>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseDefaultFiles()
    .UseStaticFiles()
    .UseRouting()
    .UseAuthorization()
    .UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });

app.Run();

// Bot implementation
public class InterviewBot : ActivityHandler
{
    private readonly Dictionary<string, int> _userStates = new Dictionary<string, int>();
    private readonly List<string> _questions = new List<string>
    {
        "Welcome to your interview! Let's begin. Please tell me about yourself and your background.",
        "What interests you about this position?",
        "Can you describe a challenging project you've worked on?",
        "How do you handle working under pressure?",
        "Where do you see yourself in 5 years?",
        "Do you have any questions for us?"
    };

    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        var userId = turnContext.Activity.From.Id;
        var userMessage = turnContext.Activity.Text.ToLower().Trim();

        if (userMessage.Contains("start interview") || userMessage.Contains("begin interview"))
        {
            _userStates[userId] = 0;
            await turnContext.SendActivityAsync(MessageFactory.Text(_questions[0]), cancellationToken);
            _userStates[userId] = 1;
        }
        else if (_userStates.ContainsKey(userId) && _userStates[userId] > 0)
        {
            var currentQuestion = _userStates[userId];
            
            if (currentQuestion < _questions.Count)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text($"Thank you for your response. Here's question {currentQuestion + 1}:"), cancellationToken);
                await turnContext.SendActivityAsync(MessageFactory.Text(_questions[currentQuestion]), cancellationToken);
                _userStates[userId] = currentQuestion + 1;
            }
            else
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Thank you for completing the interview! Your responses have been recorded. We'll be in touch soon."), cancellationToken);
                _userStates.Remove(userId);
            }
        }
        else
        {
            await turnContext.SendActivityAsync(MessageFactory.Text("Hello! Say 'start interview' to begin your interview."), cancellationToken);
        }
    }

    protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
    {
        var welcomeText = "Welcome to the AI Interview Bot! Say 'start interview' when you're ready to begin.";
        foreach (var member in membersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText), cancellationToken);
            }
        }
    }
}

// Error handler adapter
public class AdapterWithErrorHandler : BotFrameworkHttpAdapter
{
    public AdapterWithErrorHandler(BotFrameworkAuthentication auth, ILogger<BotFrameworkHttpAdapter> logger)
        : base(auth, logger)
    {
        OnTurnError = async (turnContext, exception) =>
        {
            logger.LogError(exception, $"[OnTurnError] unhandled error : {exception.Message}");
            await turnContext.SendActivityAsync("The bot encountered an error or bug.");
        };
    }
}

// Controller
[Route("api/messages")]
[ApiController]
public class BotController : ControllerBase
{
    private readonly IBotFrameworkHttpAdapter _adapter;
    private readonly IBot _bot;

    public BotController(IBotFrameworkHttpAdapter adapter, IBot bot)
    {
        _adapter = adapter;
        _bot = bot;
    }

    [HttpPost, HttpGet]
    public async Task PostAsync()
    {
        await _adapter.ProcessAsync(Request, Response, _bot);
    }
} 