namespace BaseSDK
{
    public class ApiResponse<T>
    {
        public T Data
        {
            get;
            set;
        }

        public Error Error
        {
            get;
            set;
        }

        public ApiResponse()
        {
        }
    }
}