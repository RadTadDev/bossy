namespace Bossy.Frontend
{
    public interface ICliDisplayAdapter
    {
        public string Display(object value);

        public bool OwnsRead();

        public object Read(string input);
    }
}