namespace DynamicProxyTests.TestObjects
{
    public interface IInt04
    {
        string StringReadProp { get; }
        string StringWriteProp { set; }
        string StringReadWriteProp { get;  set; }
        int IntReadProp { get; }
        int IntWriteProp { set; }
        int IntReadWriteProp { get; set; } 
    }
    // ReSharper disable ConvertToAutoProperty
    // ReSharper disable UnassignedField.Local
    // ReSharper disable InconsistentNaming
    public class Obj04
    {
        private string _StringReadProp;

        public string StringReadProp { get { return _StringReadProp; } }
        private string _StringWriteProp;
        public string StringWriteProp { set { _StringWriteProp = value; } }
        private string _StringReadWriteProp;

        public string StringReadWriteProp
        {
            get { return _StringReadWriteProp; }
            set { _StringReadWriteProp = value; }
        }
        private int _IntReadProp;
        public int IntReadProp { get { return _IntReadProp; } }
        private int _IntWriteProp;
        public int IntWriteProp { set { _IntWriteProp = value; } }
        private int _IntReadWriteProp;
        public int IntReadWriteProp
        {
            get { return _IntReadWriteProp; }
            set { _IntReadWriteProp = value; }
        } 
    }


    public class Obj04Proxy : IInt04
    {
        private readonly Obj04 _source;

        public Obj04Proxy(Obj04 source)
        {
            _source = source;
        }
        string IInt04.StringReadProp { get { return _source.StringReadProp; } }
        string IInt04.StringWriteProp { set { _source.StringWriteProp = value; } }
        string IInt04.StringReadWriteProp
        {
            get { return _source.StringReadWriteProp; }
            set { _source.StringReadWriteProp = value; }
        }
        int IInt04.IntReadProp { get { return _source.IntReadProp; } }
        int IInt04.IntWriteProp { set { _source.IntWriteProp = value; } }
        int IInt04.IntReadWriteProp
        {
            get { return _source.IntReadWriteProp; }
            set { _source.IntReadWriteProp = value; }
        }
    }

    // ReSharper restore InconsistentNaming
    // ReSharper restore UnassignedField.Local
    // ReSharper restore ConvertToAutoProperty
}