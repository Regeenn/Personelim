namespace Personelim.Helpers
{
    public class ServiceResponse<T>
    {
        public bool Success { get; set; }
        public required string Message { get; set; }
        public T? Data { get; set; }
        public List<string> Errors { get; set; }

        public ServiceResponse()
        {
            Errors = new List<string>();
        }

        public static ServiceResponse<T> SuccessResult(T data, string message = "İşlem başarılı")
        {
            return new ServiceResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ServiceResponse<T> ErrorResult(string message, List<string>? errors = null)
        {
            return new ServiceResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }

        public static ServiceResponse<T> ErrorResult(string message, string error)
        {
            return new ServiceResponse<T>
            {
                Success = false,
                Message = message,
                Errors = new List<string> { error }
            };
        }
    }
}