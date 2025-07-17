# Contributing to IntLimiter

First off, thank you for considering contributing to IntLimiter! It's people like you that make this project great.

## Code of Conduct

This project and everyone participating in it is governed by our Code of Conduct. By participating, you are expected to uphold this code.

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check the issue list as you might find out that you don't need to create one. When you are creating a bug report, please include as many details as possible:

- **Use a clear and descriptive title**
- **Describe the exact steps to reproduce the problem**
- **Provide specific examples to demonstrate the steps**
- **Describe the behavior you observed after following the steps**
- **Explain which behavior you expected to see instead and why**
- **Include screenshots and animated GIFs if possible**

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion, please include:

- **Use a clear and descriptive title**
- **Provide a step-by-step description of the suggested enhancement**
- **Provide specific examples to demonstrate the steps**
- **Describe the current behavior and explain which behavior you expected to see instead**
- **Explain why this enhancement would be useful**

### Pull Requests

- Fill in the required template
- Do not include issue numbers in the PR title
- Include screenshots and animated GIFs in your pull request whenever possible
- Follow the C# and XAML styleguides
- Include thoughtfully-worded, well-structured tests
- Document new code based on the Documentation Styleguide
- End all files with a newline

## Development Setup

### Prerequisites

- Visual Studio 2022 or later
- .NET 7 SDK
- Git for Windows
- Administrator privileges (for testing ETW and WFP features)

### Setting Up Development Environment

1. **Fork and Clone**
   ```bash
   git clone https://github.com/yourusername/intlimiter.git
   cd intlimiter
   ```

2. **Install Dependencies**
   ```bash
   dotnet restore
   ```

3. **Build Project**
   ```bash
   dotnet build
   ```

4. **Run Tests**
   ```bash
   dotnet test
   ```

5. **Run Application**
   ```bash
   # Must run as Administrator for full functionality
   dotnet run --project IntLimiter
   ```

### Project Structure

```
IntLimiter/
├── Models/          # Data models and entities
├── Services/        # Business logic and system integration
├── ViewModels/      # MVVM view models
├── Views/           # XAML views and dialogs
├── Resources/       # Themes, styles, and resources
└── Helpers/         # Utility classes and helpers
```

## Coding Standards

### C# Guidelines

- Follow [Microsoft's C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use PascalCase for public members
- Use camelCase for private fields with underscore prefix (`_fieldName`)
- Use explicit access modifiers
- Add XML documentation for public APIs

```csharp
/// <summary>
/// Represents a network bandwidth rule for a specific process
/// </summary>
public class BandwidthRule
{
    private readonly int _processId;
    
    /// <summary>
    /// Gets the process ID associated with this rule
    /// </summary>
    public int ProcessId => _processId;
    
    /// <summary>
    /// Sets the download limit for the process
    /// </summary>
    /// <param name="limitInBps">Limit in bytes per second</param>
    public void SetDownloadLimit(long limitInBps)
    {
        // Implementation
    }
}
```

### XAML Guidelines

- Use proper indentation (4 spaces)
- Group related properties
- Use data binding instead of code-behind when possible
- Follow MVVM pattern strictly

```xml
<Button Content="Apply Limit"
        Command="{Binding ApplyLimitCommand}"
        CommandParameter="{Binding SelectedProcess}"
        Style="{StaticResource ModernButton}"
        Margin="5"
        IsEnabled="{Binding CanApplyLimit}"/>
```

### Architecture Guidelines

- **MVVM Pattern:** Strict separation of concerns
- **Dependency Injection:** Use constructor injection for services
- **Error Handling:** Proper exception handling and logging
- **Threading:** Use async/await for long-running operations
- **Resource Management:** Implement IDisposable properly

## Testing

### Unit Tests

- Write unit tests for all business logic
- Use xUnit testing framework
- Mock external dependencies
- Aim for 80%+ code coverage

```csharp
[Fact]
public void BandwidthLimiter_SetsLimit_Success()
{
    // Arrange
    var limiter = new BandwidthLimiterService();
    var processId = 1234;
    
    // Act
    var result = limiter.SetProcessLimit(processId, 1000000, 500000);
    
    // Assert
    Assert.True(result);
    Assert.Equal(1000000, limiter.GetDownloadLimit(processId));
}
```

### Integration Tests

- Test service interactions
- Test database operations
- Test UI workflows (where applicable)

## Documentation

### Code Documentation

- Use XML documentation for public APIs
- Include parameter descriptions and return values
- Provide usage examples for complex methods

### README Updates

- Update README.md for new features
- Include screenshots for UI changes
- Update installation instructions if needed

## Submitting Changes

### Pull Request Process

1. **Create Feature Branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make Changes**
   - Follow coding standards
   - Add tests for new functionality
   - Update documentation

3. **Commit Changes**
   ```bash
   git commit -m "Add feature: your feature description"
   ```

4. **Push to Fork**
   ```bash
   git push origin feature/your-feature-name
   ```

5. **Create Pull Request**
   - Use clear title and description
   - Reference related issues
   - Include screenshots if applicable

### Commit Message Guidelines

- Use present tense ("Add feature" not "Added feature")
- Use imperative mood ("Move cursor to..." not "Moves cursor to...")
- Limit first line to 72 characters
- Reference issues and pull requests liberally after the first line

Examples:
```
Add bandwidth limiting for UDP connections

- Implement UDP traffic control in WFPService
- Add unit tests for UDP limiting
- Update UI to show UDP limits

Fixes #123
```

## Release Process

### Versioning

We use [Semantic Versioning](https://semver.org/):
- **MAJOR:** Incompatible API changes
- **MINOR:** New functionality (backwards compatible)
- **PATCH:** Bug fixes (backwards compatible)

### Release Checklist

- [ ] Update version numbers
- [ ] Update CHANGELOG.md
- [ ] Test on clean Windows installation
- [ ] Create GitHub release
- [ ] Update documentation

## Getting Help

### Community Resources

- **GitHub Issues:** Bug reports and feature requests
- **GitHub Discussions:** General questions and discussions
- **Project Wiki:** Detailed documentation and guides

### Development Questions

For development-related questions:
1. Check existing issues and discussions
2. Review project documentation
3. Create new discussion if needed

## Recognition

Contributors will be recognized in:
- README.md contributors section
- Release notes
- Project documentation

Thank you for contributing to IntLimiter! 🚀