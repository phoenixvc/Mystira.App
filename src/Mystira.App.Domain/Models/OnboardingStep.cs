namespace Mystira.App.Domain.Models;

public class OnboardingStep
{
    public int StepNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public OnboardingStepType Type { get; set; }
}

public enum OnboardingStepType
{
    Welcome,
    ProfileCreation,
    TutorialScenarioGeneration,
    TutorialContentDisplay,
    TutorialDiceMechanic,
    Complete
}
