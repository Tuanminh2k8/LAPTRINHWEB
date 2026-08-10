namespace Source.Services
{
    public class OperationDemoService
    {
        private readonly ISingletonOperation _singletonOperation;
        private readonly IScopedOperation _scopedOperation;
        private readonly ITransientOperation _transientOperation;

        public OperationDemoService(
            ISingletonOperation singletonOperation,
            IScopedOperation scopedOperation,
            ITransientOperation transientOperation)
        {
            _singletonOperation = singletonOperation;
            _scopedOperation = scopedOperation;
            _transientOperation = transientOperation;
        }

        public Guid SingletonId => _singletonOperation.OperationId;
        public Guid ScopedId => _scopedOperation.OperationId;
        public Guid TransientId => _transientOperation.OperationId;
    }
}
