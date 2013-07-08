namespace Shared
{
    public interface IPool<T>
    {
        T GetItem();
        void PutBackItem(T item);
    }
}