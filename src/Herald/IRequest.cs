namespace Herald;

/// <summary>
/// Marker interface for a request that produces a <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IRequest<out TResponse>;
