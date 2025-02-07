namespace user_service.Exceptions
{
    public class DuplicateEmailException : Exception
    {
        public DuplicateEmailException(string message = "This email is already in use") : base(message) { }
    }
}
