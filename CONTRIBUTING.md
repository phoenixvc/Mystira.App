# Contributing to Mystira

First off, thank you for considering contributing to Mystira! We welcome any help you can offer, from fixing bugs to proposing new features.

## How to Contribute

We follow a standard GitHub fork-and-pull-request workflow.

### 1. Set Up Your Environment

- **Fork the repository** on GitHub.
- **Clone your fork** to your local machine:
  ```bash
  git clone https://github.com/YOUR_USERNAME/Mystira.App.git
  cd Mystira.App
  ```
- **Follow the instructions** in the main `README.md` to set up your local development environment.

### 2. Create a Branch

Create a new branch for your feature or bugfix. Use a descriptive name that reflects the work you're doing.

```bash
git checkout -b feature/your-new-feature
# or
git checkout -b fix/bug-description
```

### 3. Make Your Changes

- **Write clean code:** Follow the existing code style and architectural patterns. Our goal is to maintain a consistent and readable codebase.
- **Add tests:** All new features and bug fixes should be accompanied by relevant unit or integration tests to ensure they work as expected and prevent future regressions.
- **Update documentation:** If you're adding a new feature or changing an existing one, please update the relevant documentation (e.g., `README.md`, API docs) to reflect your changes.

### 4. Commit and Push

- **Commit your changes** with a clear and concise commit message. We follow the [Conventional Commits](https://www.conventionalcommits.org/) specification.
  ```bash
  git commit -m "feat: Add guardian dashboard feature"
  git commit -m "fix: Correct CORS policy vulnerability"
  ```
- **Push your branch** to your fork on GitHub:
  ```bash
  git push origin feature/your-new-feature
  ```

### 5. Create a Pull Request

- Go to the original Mystira repository and you should see a prompt to create a pull request from your new branch.
- Provide a clear title and a detailed description of the changes you've made.
- Link to any relevant issues.
- Our team will review your pull request, provide feedback, and merge it once it's ready.

## Best Practices

- **Security:** Always consider the security implications of your changes. Ensure all inputs are validated and follow best practices for authentication and data protection.
- **Performance:** Write efficient code and consider the performance impact of your changes, especially for database queries and frontend rendering.
- **Accessibility:** For any UI changes, ensure they meet WCAG 2.1 AA accessibility standards.

Thank you again for your contribution!
