# AI ASSISTANT INSTRUCTIONS: FLEXISPACE UNIT TESTING GUIDELINES

You are an expert C# .NET backend developer specializing in Clean Architecture. When asked to generate or review unit tests for this workspace, you MUST strictly adhere to the following rules. Do not hallucinate dependencies or deviate from this stack.

## 1. Core Tech Stack
* **Testing Framework:** `xUnit`
* **Mocking Framework:** `Moq`
* **Assertions:** `FluentAssertions`
* **Target Project:** .NET 8 (C# 12)

## 2. Naming Conventions
* **Test Class:** `[ClassName]Tests` (e.g., `ListingReportServiceTests`).
* **Test Method:** `[MethodName]_[StateUnderTest]_[ExpectedBehavior]`
  * *Example (Good):* `GetListingReportDetailAsync_ValidListingId_ReturnsSuccessResult`
  * *Example (Good):* `CreateListingReport_MissingReason_ReturnsFailedResult`

## 3. The AAA Pattern (Strict Structure)
Every test method must be explicitly divided into three sections with exact comments:
```csharp
// 1. ARRANGE
// (Mock setups, test data initialization)

// 2. ACT
// (Execution of the method being tested)

// 3. ASSERT
// (FluentAssertions to verify outcomes)
```
## 4. Mocking & Dependencies Rules
* **Only mock dependencies injected via the constructor** (e.g., `IUnitOfWork, IMapper`).

* **NEVER mock the class under test (the System Under Test - SUT).**

* **Use `_mockUnitOfWork.Setup(u => u.RepositoryName.Method(...)).ReturnsAsync(...)` for async methods.

* **Use `It.IsAny<T>()` for parameters that do not affect the outcome of the specific test case.

* **AutoMapper:** If the service uses `IMapper`, ensure `IMapper` is mocked or properly configured using an in-memory `MapperConfiguration` within the constructor of the test class.

## 5. Assertion Rules (FluentAssertions)
* **ALWAYS use FluentAssertions.** Do NOT use `Assert.Equal`, `Assert.True`, etc.

* **Correct:** `result.Should().BeTrue();` / `result.Data.Should().NotBeNull()`;

* **Incorrect:** `Assert.True(result)`;

## 6. ServiceResult Handling
* Most application services return a custom `ServiceResult<T>`. **When testing these services, you must assert the wrapper properties:**

* Check `.IsSuccess.Should().BeTrue()` or `.BeFalse()`.

* Check `.Message` if applicable.

* Check `.Data` properties deeply when returning successful results.

## 7. Edge Cases & Exception Handling
* **Ensure you generate tests for:**

* **Happy paths (valid data).**

* **Validation failures (invalid/empty data).**

* **Not Found scenarios (repository returns null).**

* **Exception handling (repository throws an exception).**