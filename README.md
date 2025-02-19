## todoAppRoadmap

### Version 1.0

- Initial release with core functionalities:
  - User registration and authentication.
  - Implement user roles (e.g., Admin, User).
  - Task creation, retrieval, updating, and deletion.
  - Basic frontend UI with React.
  - Implement logging and monitoring with OpenTelemetry - Otel Collector - Grafana - Prometheus - Loki - Jaeger.

### Version 1.1

- **Enhancements**:
  - Improve UI/UX with responsive design enhancements.
  - Integrate third-party APIs for extended functionalities.

### Version 1.2

- **Improvements**:
  - Optimize performance and scalability.
  - Add more comprehensive end-to-end tests.
  - Implement more detailed logging and monitoring with OpenTelemetry.
  - Enhance security measures.

### Future Plans

- **Advanced Features**:
  - Add calendar integration.
  - Introduce mobile support with React Native.
  - Expand to support multiple languages.

## Getting Started

Follow these instructions to set up and run the project locally.

### Prerequisites

Ensure you have the following installed on your machine:

- **Node.js**: [Download & Install](https://nodejs.org/) (v18.x or later)
- **npm**: Comes with Node.js
- **.NET 6 SDK**: [Download & Install](https://dotnet.microsoft.com/download/dotnet/6.0)
- **Docker**: [Download & Install](https://www.docker.com/get-started) (optional, for containerization)
- **Git**: [Download & Install](https://git-scm.com/downloads)

### Installation

1. **Clone the Repository**

   ```bash
   git clone https://github.com/kemalcankurt/todoApp.git
   cd todoApp
   ```

2. **Setup Frontend**

   ```bash
   cd frontend
   npm install
   cd ..
   ```

3. **Setup Backend Services**

   For each backend service (e.g., `user-service`, `todo-service`, `api-gateway`), navigate to the service directory and restore dependencies.

   ```bash
   cd services/user-service
   dotnet restore
   cd ../todo-service
   dotnet restore
   cd ../api-gateway
   dotnet restore
   cd ../../
   ```

4. **Setup Tests**

   ```bash
   cd tests/playwright
   npm install
   cd ../../
   ```

### Running the Project

You can run the project using Docker Compose or manually start each service.

#### Using Docker Compose

1. **Start All Services**

   ```bash
   docker-compose up --build
   ```

   This command will build and run all services, including the frontend, backend services, and the API gateway.

2. **Access the Application**

   - **Frontend**: [http://localhost:5173](http://localhost:5173)
   - **API Gateway**: [http://localhost:8000](http://localhost:8000)
   - **Swagger UI** (for backend services):
     - User Service: [http://localhost:8000/swagger/index.html?urls.primaryName=User+Service+-+v1](http://localhost:8000/swagger/index.html?urls.primaryName=User+Service+-+v1)
     - Todo Service: [http://localhost:8000/swagger/index.html?urls.primaryName=Todo+Service+-+v1](http://localhost:8000/swagger/index.html?urls.primaryName=Todo+Service+-+v1)
     - API Gateway Swagger: [http://localhost:8000/swagger](http://localhost:8000/swagger)

#### Running Services Manually

1. **Start Backend Services**

   Open separate terminal windows/tabs for each service.

   - **User Service**

     ```bash
     cd services/user-service
     dotnet run
     ```

   - **Todo Service**

     ```bash
     cd services/todo-service
     dotnet run
     ```

   - **API Gateway**

     ```bash
     cd services/api-gateway
     dotnet run
     ```

2. **Start Frontend**

   ```bash
   cd frontend
   npm run dev
   ```

3. **Access the Application**

   - **Frontend**: [http://localhost:5173](http://localhost:5173)
   - **API Gateway**: [http://localhost:8000](http://localhost:8000)

### Running Tests

#### Playwright End-to-End Tests

1. **Navigate to the Playwright Test Directory**

   ```bash
   cd tests/playwright
   ```

2. **Run Tests**

   ```bash
   npx playwright test
   ```

   To generate reports in HTML, JSON, and JUnit formats:

   ```bash
   npx playwright test --reporter=html,json,junit
   ```

#### .NET Unit Tests

1. **Navigate to the User Service Test Directory**

   ```bash
   cd services/user-service.Tests
   ```

2. **Run Tests**

   ```bash
   dotnet test
   ```

## Contributing

Contributions are welcome! Follow these steps to contribute to the project:

1. **Fork the Repository**

   Click the [Fork](https://github.com/kemalcankurt/todoApp/fork) button on the repository page.

2. **Clone Your Fork**

   ```bash
   git clone https://github.com/yourusername/todoApp.git
   cd todoApp
   ```

3. **Create a New Branch**

   ```bash
   git checkout -b feature/YourFeatureName
   ```

4. **Make Your Changes**

   Implement your feature or fix a bug, ensuring code quality and adherence to project standards.

5. **Commit Your Changes**

   ```bash
   git add .
   git commit -m "Add your descriptive commit message"
   ```

6. **Push to Your Fork**

   ```bash
   git push origin feature/YourFeatureName
   ```

7. **Create a Pull Request**

   Navigate to the original repository and create a pull request from your fork's branch.

## License

This project is licensed under the [Apache License 2.0](LICENSE).

## Acknowledgments

- [React](https://reactjs.org/)
- [Vite](https://vitejs.dev/)
- [TypeScript](https://www.typescriptlang.org/)
- [.NET Core](https://dotnet.microsoft.com/)
- [Ocelot API Gateway](https://ocelot.readthedocs.io/)
- [Serilog](https://serilog.net/)
- [OpenTelemetry](https://opentelemetry.io/)
- [Playwright](https://playwright.dev/)
- [Tailwind CSS](https://tailwindcss.com/)
- [Axios](https://axios-http.com/)
- [Playwright](https://playwright.dev/)
- [Dotnet Entity Framework](https://learn.microsoft.com/en-us/ef/)

Feel free to customize and expand this README to better suit your project's specifics and additional features.
