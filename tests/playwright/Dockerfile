# Use the latest Playwright base image matching the project version
FROM mcr.microsoft.com/playwright:v1.50.1-jammy

WORKDIR /app

# Copy package.json and package-lock.json first
COPY package.json package-lock.json ./

# Install dependencies
RUN npm install

# Copy test files
COPY . .

# Run tests and generate reports
CMD ["npx", "playwright", "test", "--reporter", "html,json,junit"]

