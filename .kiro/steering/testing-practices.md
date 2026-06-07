# Testing Practices

Based on [Microsoft's Unit Testing Best Practices for .NET](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices).

## Test Framework
- **Framework**: xUnit.
- **Mocking**: Moq (for interfaces like `IBlockAccessor`, `ICoreServerAPI`).
- **Project**: Separate test project (`HotSpringsExpanded.Tests`) referencing the main project.

## Test Naming Convention

Test names follow the three-part pattern: `MethodName_Scenario_ExpectedBehavior`.

Numbers below 10 in test names must use their word form (Zero, One, Two, Three, etc.) instead of digits:

```csharp
// ✅ Good
public void GetTemperature_BlockWith87Deg_Returns87()
public void IsMeltableBlock_NullBlock_ReturnsFalse()
public void MeltingChance_AtDistanceZero_ReturnsMax()
public void Register_SamePositionTwice_OverwritesExisting()

// ❌ Avoid
public void Test1()
public void TestGetTemperature()
public void MeltingChance_AtDistance0_ReturnsMax()
```

## Arrange, Act, Assert (AAA)

Every test must follow the AAA pattern with clear separation:

```csharp
[Fact]
public void GetSnowLevel_FullSnowBlock_Returns8()
{
    // Arrange
    Block block = CreateBlockWithPath("snow");

    // Act
    int level = processor.GetSnowLevel(block);

    // Assert
    Assert.Equal(8, level);
}
```

## Key Principles

### Write minimally passing tests.
Use the simplest input that verifies the behaviour. Don't set extra properties or use non-zero values unless required.

### Avoid magic strings and numbers.
Assign hard-coded test values to named constants:

```csharp
private const int Temperature87 = 87;
private const double ExpectedMaxChance = 0.9;
```

### Avoid logic in tests.
No `if`, `for`, `while`, or `switch` in test methods. Use `[Theory]` with `[InlineData]` for parameterised tests instead of loops.

```csharp
[Theory]
[InlineData(55, 1)]
[InlineData(65, 2)]
[InlineData(74, 3)]
[InlineData(87, 4)]
public void GetRadiusForTemperature_KnownTemperature_ReturnsExpectedRadius(int temperature, int expectedRadius)
{
    // Act
    int radius = HotSpringTracker.GetRadiusForTemperature(temperature);

    // Assert
    Assert.Equal(expectedRadius, radius);
}
```

### One Act per test.
Each test should have a single Act step. If you need to test multiple scenarios, use `[Theory]` or separate test methods.

### Validate private methods through public methods.
Don't test private methods directly. Test the public method that calls them and verify the observable outcome.

### Use helper methods instead of Setup/Teardown.
Create helper methods for common object construction. This keeps each test self-contained and readable:

```csharp
private static Block CreateBlockWithPath(string path)
{
    // Create a mock block with the given code path.
}

private HotSpringTracker CreateTrackerWithHotSpring(BlockPos position, int temperature)
{
    HotSpringTracker tracker = new HotSpringTracker();
    tracker.RegisterHotSpring(position, temperature);
    return tracker;
}
```

### Avoid infrastructure dependencies.
Unit tests must not depend on the file system, network, or game engine. Mock all external dependencies (API, block accessors, world).

### Handle static references with seams.
When code depends on static state (e.g., `api.World.Rand`), inject the dependency so tests can control it.

## Test Organisation

### File structure.
Mirror the source project structure in the test project:

```
HotSpringsExpanded.Tests/
├── Utilities/
│   ├── BlockIdentifiersTests.cs
│   └── BlockPosTests.cs
├── Tracking/
│   ├── HotSpringTrackerTests.cs
│   └── SnowBlockTrackerTests.cs
├── Processing/
│   ├── MeltingProcessorTests.cs
│   └── MeltSnowSystemTests.cs
└── Models/
    (No tests needed — pure data classes with no logic.)
```

### One test class per source class.
Each source class gets its own test class. Group related tests within the class using regions or comment headers if needed.

## Characteristics of Good Unit Tests

- **Fast**: Tests run in milliseconds. No real world access.
- **Isolated**: No shared mutable state between tests.
- **Repeatable**: Same result every run regardless of environment.
- **Self-checking**: Pass/fail determined by assertions, not human inspection.
- **Timely**: Tests should not take disproportionately long to write.

## Mocking Guidelines

### What to mock.
- `ICoreServerAPI` and its sub-interfaces (`IWorldAccessor`, `IBlockAccessor`).
- `Block` objects (create fakes with controlled `Code.Path` values).
- Random number generators (inject or mock `api.World.Rand`).

### What not to mock.
- Value types and structs (`BlockPos`).
- Pure static utility methods (`BlockIdentifiers`) — test them directly.
- Data classes (`HotSpring`, `SnowBlockData`) — instantiate them directly.
