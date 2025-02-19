namespace user_service.Swagger
{
    public class ApiSwaggerOptions
    {
        public string Title { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public ApiSwaggerContact Contact { get; set; }
        public ApiSwaggerSecurity Security { get; set; }
    }

    public class ApiSwaggerContact
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public class ApiSwaggerSecurity
    {
        public ApiSwaggerBearer Bearer { get; set; }
    }

    public class ApiSwaggerBearer
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Scheme { get; set; }
        public string BearerFormat { get; set; }
        public string In { get; set; }
        public string Type { get; set; }
    }
}