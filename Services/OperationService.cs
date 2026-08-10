namespace Source.Services
{
    public interface ISingletonOperation
    {
        Guid OperationId { get; }
    }

    public interface IScopedOperation
    {
        Guid OperationId { get; }
    }

    public interface ITransientOperation
    {
        Guid OperationId { get; }
    }

    public class OperationService : ISingletonOperation, IScopedOperation, ITransientOperation
    {
        public Guid OperationId { get; } = Guid.NewGuid();
    }
}
