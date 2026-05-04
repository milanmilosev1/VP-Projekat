namespace SmartGrid.Common
{
    public interface IValidator<T> where T : class
    {
        void Validate(T t);
    }
}
