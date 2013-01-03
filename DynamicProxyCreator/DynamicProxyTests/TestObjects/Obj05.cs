namespace com.bodurov.DynamicProxyTests.TestObjects
{
    public interface IInt05<T1,T2>
    {
        T1 ReadProp { get; }
        T1 WriteProp { set; }
        T1 ReadWriteProp { get; set; }
        T2 ReadProp2 { get; }
        T2 WriteProp2 { set; }
        T2 ReadWriteProp2 { get; set; } 
    }

    // ReSharper disable ConvertToAutoProperty
    // ReSharper disable UnassignedField.Local
    // ReSharper disable InconsistentNaming
    public class Obj05<T1, T2>
    {
        private T1 _ReadProp;
        public T1 ReadProp { get { return _ReadProp; } }
        private T1 _WriteProp;
        public T1 WriteProp { set { _WriteProp = value; } }
        private T1 _ReadWriteProp;
        public T1 ReadWriteProp
        {
            get { return _ReadWriteProp; }
            set { _ReadWriteProp = value; }
        }
        private T2 _ReadProp2;
        public T2 ReadProp2 { get { return _ReadProp2; } }
        private T2 _WriteProp2;
        public T2 WriteProp2 { set { _WriteProp2 = value; } }
        private T2 _ReadWriteProp2;
        public T2 ReadWriteProp2
        {
            get { return _ReadWriteProp2; }
            set { _ReadWriteProp2 = value; }
        } 
    }
    // ReSharper restore InconsistentNaming
    // ReSharper restore UnassignedField.Local
    // ReSharper restore ConvertToAutoProperty
}