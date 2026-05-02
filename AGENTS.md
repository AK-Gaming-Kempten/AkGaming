# Repository Coding Guidelines

## API Projects

- Prefer MVC controllers over Minimal APIs for HTTP endpoints unless a project already has a clear Minimal API convention.
- Controller actions should use block-bodied methods with explicit local variables and `return` statements. Avoid expression-bodied (`=>`) controller actions, even for simple pass-through calls, so actions remain easy to extend and debug.
- Controller unit tests should use Moq to mock service interfaces. Do not create large hand-written service doubles for controller tests.

## Test Projects

- Split tests by target layer and responsibility, for example `Tests/Application`, `Tests/Domain`, `Tests/Infrastructure`, and `Tests/WebApi`.
- Keep test classes small and focused. Prefer multiple controller/service-specific fixtures over one large catch-all fixture.
- Add a `[Description("...")]` attribute to each test explaining the behavior it verifies.
- Separate each test body with explicit `// Arrange`, `// Act`, and `// Assert` comments.
- Use shared helpers, setup methods, and fixture properties for common arrange logic and dependencies instead of repeating large object graphs in every test. Promote common mocks, services, controllers, stores, and other fixture-level dependencies to class properties initialized by setup methods.

## Blazor/Razor Projects

- Prefer reusable Razor components for repeated UI patterns, repeated forms, repeated cards, and repeated loop markup. Keep pages focused on orchestration and state, and move reusable rendering into components with clear parameters.
- Keep C# component logic in code-behind `.razor.cs` files. Avoid `@code` blocks in `.razor` files except for trivial markup-only parameter declarations when a code-behind file would add no value.
- Avoid nesting `@if`/`@else` blocks deeply in Blazor markup. When conditional markup starts to layer past two levels, move the inner block into a dedicated component such as a `DetailsView` so the page stays readable.
- Follow the established frontend interaction patterns before inventing new page structures. Prefer selectors/dropdowns and focused detail views like the existing teams, player profiles, and game administration flows over crowded all-in-one admin pages.
- For creation flows inside admin selectors, keep create forms collapsed by default and reveal them only after an explicit user action such as a `Create` button in the selector menu.
