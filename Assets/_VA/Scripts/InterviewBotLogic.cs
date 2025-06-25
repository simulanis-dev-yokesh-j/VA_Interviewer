using System;
using System.Collections.Generic;
using UnityEngine;

public class InterviewBotLogic : MonoBehaviour
{
    [System.Serializable]
    public class InterviewQuestion
    {
        public string question;
        public string category; // Technical, Behavioral, Experience, etc.
        public int timeLimit; // in seconds
        public bool isRequired;
    }

    [Header("Interview Configuration")]
    public List<InterviewQuestion> questions = new List<InterviewQuestion>();
    
    private int currentQuestionIndex = 0;
    private Dictionary<int, string> userResponses = new Dictionary<int, string>();
    private bool interviewStarted = false;
    private bool interviewCompleted = false;

    void Start()
    {
        InitializeDefaultQuestions();
    }

    private void InitializeDefaultQuestions()
    {
        questions.Add(new InterviewQuestion 
        { 
            question = "Hello! Welcome to the interview. Please tell me about yourself and your background.", 
            category = "Introduction", 
            timeLimit = 120, 
            isRequired = true 
        });
        
        questions.Add(new InterviewQuestion 
        { 
            question = "What interests you about this position?", 
            category = "Motivation", 
            timeLimit = 90, 
            isRequired = true 
        });
        
        questions.Add(new InterviewQuestion 
        { 
            question = "Can you describe a challenging project you've worked on?", 
            category = "Experience", 
            timeLimit = 120, 
            isRequired = true 
        });
        
        questions.Add(new InterviewQuestion 
        { 
            question = "How do you handle working under pressure?", 
            category = "Behavioral", 
            timeLimit = 90, 
            isRequired = true 
        });
        
        questions.Add(new InterviewQuestion 
        { 
            question = "Where do you see yourself in 5 years?", 
            category = "Career Goals", 
            timeLimit = 90, 
            isRequired = true 
        });
        
        questions.Add(new InterviewQuestion 
        { 
            question = "Do you have any questions for us?", 
            category = "Closing", 
            timeLimit = 60, 
            isRequired = false 
        });
    }

    public string StartInterview()
    {
        if (!interviewStarted)
        {
            interviewStarted = true;
            currentQuestionIndex = 0;
            return "Welcome to your interview! I'll be asking you a series of questions. Please take your time to respond thoughtfully. Let's begin:\n\n" + 
                   GetCurrentQuestion();
        }
        return "Interview has already started.";
    }

    public string GetCurrentQuestion()
    {
        if (currentQuestionIndex < questions.Count)
        {
            var question = questions[currentQuestionIndex];
            return $"Question {currentQuestionIndex + 1} of {questions.Count} ({question.category}):\n{question.question}\n\n" +
                   $"You have {question.timeLimit} seconds to respond.";
        }
        return "No more questions available.";
    }

    public string ProcessUserResponse(string response)
    {
        if (!interviewStarted || interviewCompleted)
        {
            return "Please start the interview first by saying 'start interview'.";
        }

        // Store the response
        userResponses[currentQuestionIndex] = response;
        
        // Move to next question
        currentQuestionIndex++;
        
        if (currentQuestionIndex >= questions.Count)
        {
            interviewCompleted = true;
            return CompleteInterview();
        }
        else
        {
            return "Thank you for your response. Here's the next question:\n\n" + GetCurrentQuestion();
        }
    }

    private string CompleteInterview()
    {
        string summary = "Thank you for completing the interview! Here's a summary of your responses:\n\n";
        
        for (int i = 0; i < questions.Count; i++)
        {
            if (userResponses.ContainsKey(i))
            {
                summary += $"Q{i + 1}: {questions[i].question}\n";
                summary += $"Your Answer: {userResponses[i]}\n\n";
            }
        }
        
        summary += "The interview has been completed. Thank you for your time!";
        return summary;
    }

    public bool IsInterviewCompleted()
    {
        return interviewCompleted;
    }

    public int GetCurrentQuestionNumber()
    {
        return currentQuestionIndex + 1;
    }

    public int GetTotalQuestions()
    {
        return questions.Count;
    }
} 