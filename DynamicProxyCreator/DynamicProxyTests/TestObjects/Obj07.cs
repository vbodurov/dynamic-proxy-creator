namespace com.bodurov.DynamicProxyTests.TestObjects
{
    public interface IInt07
    {
        bool TryProcess(int toAdd, ref int r, out double d);
    }

    public class Obj07
    {
        public bool TryProcess(int toAdd, ref int r, out double half)
        {
            r += toAdd;

            half = r/2.0;

            return true;
        }
    }
}