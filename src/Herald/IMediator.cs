namespace Herald;

/// <summary>
/// Sends requests and publishes notifications.
/// </summary>
/// <remarks>
/// Depend on <see cref="ISender"/> or <see cref="IPublisher"/> instead when a class only needs one of them.
/// </remarks>
public interface IMediator : ISender, IPublisher;
