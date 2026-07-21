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

## Entity Framework and SQLite

- Treat SQLite as a real supported provider, not just a lightweight substitute for PostgreSQL. SQLite cannot translate ordering or comparison expressions for some CLR types, notably `DateTimeOffset` and `decimal`.
- Do not apply `OrderBy`, `ThenBy`, range comparisons, `Min`, or `Max` to unsupported SQLite types inside an EF query. First execute the translatable filtering/projection with `ToListAsync`, then perform the unsupported operation with LINQ to Objects, or store/query a provider-neutral representation when server-side ordering is required.
- Add a provider-backed SQLite repository test for queries involving `DateTimeOffset`, `decimal`, provider-specific mappings, or non-trivial ordering. Mock-only service tests do not verify EF translation and are not sufficient for these queries.

## Blazor/Razor Projects

- Prefer reusable Razor components for repeated UI patterns, repeated forms, repeated cards, and repeated loop markup. Keep pages focused on orchestration and state, and move reusable rendering into components with clear parameters.
- Keep C# component logic in code-behind `.razor.cs` files. Avoid `@code` blocks in `.razor` files except for trivial markup-only parameter declarations when a code-behind file would add no value.
- Avoid nesting `@if`/`@else` blocks deeply in Blazor markup. When conditional markup starts to layer past two levels, move the inner block into a dedicated component such as a `DetailsView` so the page stays readable.
- Follow the established frontend interaction patterns before inventing new page structures. Prefer selectors/dropdowns and focused detail views like the existing teams, player profiles, and game administration flows over crowded all-in-one admin pages.
- For creation flows inside admin selectors, keep create forms collapsed by default and reveal them only after an explicit user action such as a `Create` button in the selector menu.
- Put secondary actions for individual list or card items, such as edit, complete, archive, move, and delete, in the established `ContextMenu` component instead of rendering a permanent row of action buttons. Keep permanent buttons for clear primary actions that apply to the page or section as a whole.
- Use popup dialogs for complex, multi-field, selection, confirmation, create, and edit actions instead of expanding substantial forms inline. Reuse the established dialog components, templates, button treatments, validation presentation, spacing, and responsive styling rather than introducing one-off modal markup or CSS.
- Prefer direct drag-and-drop controls for spatial operations such as reordering items or moving/assigning items between visible containers when that interaction is applicable. Do not use an assignment or “move to” dropdown as the primary interaction when the source and destination are already visible on the page.
- Drag-and-drop interactions must include an obvious drag handle or draggable affordance, visibly highlighted drop targets, and a preview indicator showing the exact insertion or destination position before the user drops the item.
