namespace Trading_Assistant.Contracts.Services;

public interface IActivationService
{
    Task ActivateAsync(object activationArgs);
}
