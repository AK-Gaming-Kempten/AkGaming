# AkGaming Core Theme

This folder contains the shared theme assets that are synced into the management frontend and the identity UI during build.

Current shared assets:

- `wwwroot/theme/akgaming-base-theme.css`: semantic color tokens and theme mode overrides.
- `wwwroot/theme/baseStyles.css`: common form, button, panel, layout, and table primitives.
- `wwwroot/theme/theme-showcase.html`: side-by-side visual reference for valid light and dark theme surface/text combinations.

## Color Pairing Rules

The theme uses semantic layers, not free-form color picking.

- `--color-text-primary` and `--color-text-secondary` are only for `--color-background-primary` and `--color-background-secondary`.
- `--color-text-tertiary` and `--color-text-quartiary` are only for `--color-background-tertiary` and `--color-background-quartiary`.
- Use `--color-text-special` only on special-purpose accent surfaces such as buttons or explicitly branded highlights.
- Use `--color-highlight-primary` and `--color-highlight-secondary` for emphasis, focus, active states, and decorative accents. Do not treat them as general body text colors.
- Prefer semantic aliases such as `--color-button-*`, `--color-input-*`, and `--color-card-*` over raw palette tokens in feature code.

`quartiary` is spelled that way in the CSS for backwards compatibility. Keep the existing token name unless the whole theme API is migrated together.

## Fundamental Palette

These are the raw building blocks. Feature styles should usually not reference them directly.

| Token | Use |
| --- | --- |
| `--color-white` | Absolute light foreground or surface fallback. |
| `--color-black` | Absolute dark foreground or fallback. |
| `--color-light-gray` | Light neutral hover or contrast support color. |
| `--color-gray` | Neutral muted tone and inverted-surface support. |
| `--color-dark-grey` | Deep neutral surface for dark mode and inverse layers. |
| `--color-light-green-grey` | Soft brand-tinted light surface. |
| `--color-dark-green` | Deep brand surface for dark sections. |
| `--color-green` | Primary brand accent and strong action color. |
| `--color-light-green` | Lighter accent for hover and highlight states. |
| `--color-red` | Strong destructive accent. |
| `--color-red-light` | Softer destructive/default danger state. |

## Semantic Tokens

### Text

| Token | Use Case |
| --- | --- |
| `--color-text-primary` | Main copy on primary and secondary surfaces. |
| `--color-text-secondary` | Supporting copy, labels, and metadata on primary and secondary surfaces. |
| `--color-text-tertiary` | Main copy on tertiary and quartiary surfaces. |
| `--color-text-quartiary` | Supporting or inverse copy on tertiary and quartiary surfaces. |
| `--color-text-special` | Text placed on branded action surfaces such as primary buttons. |
| `--color-text-special-secondary` | Reduced-contrast support text inside special-purpose UI. |

### Backgrounds

| Token | Use Case |
| --- | --- |
| `--color-background-primary` | Default content surfaces, cards, inputs, and tables. |
| `--color-background-secondary` | Secondary panels, page shells, and subtle tinted containers. |
| `--color-background-tertiary` | Strong inverse brand surface. Pair with tertiary/quartiary text tokens. |
| `--color-background-quartiary` | Strong neutral inverse surface. Pair with tertiary/quartiary text tokens. |
| `--color-background-special` | Branded special-purpose surface, usually for nav or emphasized controls. |

### Highlights

| Token | Use Case |
| --- | --- |
| `--color-highlight-primary` | Hover, focus rings, selected accents, and lighter emphasis. |
| `--color-highlight-secondary` | Primary calls to action, strong emphasis, and confirmed-positive accents. |

## Derived Component Tokens

These should be the default entry point for feature work.

| Token Group | Use Case |
| --- | --- |
| `--color-button-*` | Button backgrounds, text, hover states, and disabled states. |
| `--color-input-*` | Input backgrounds, borders, text, and focus borders. |
| `--color-card-*` | Panel backgrounds, borders, shadows, and hover treatments. |
| `--color-status-*` | Status chips, dues states, and table-level state accents. |

## Usage Guidance

- Build page shells with `background-secondary` and panel bodies with `background-primary`.
- Put dense information inside panels or tables from `baseStyles.css` instead of ad hoc flex rows.
- When a page needs an inverse or dark section, switch both the background token and the text token family together.
- If a new component needs a color that does not fit these rules, add or derive a semantic token here first instead of bypassing the system in app code.
