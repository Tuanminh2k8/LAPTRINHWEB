# Sales Variant Manager

Reusable Razor + CSS + JavaScript component reproducing the approved sales-variant UI.

## Use in ASP.NET Core Razor

1. Put the files in your project, e.g.:
   - `Views/Shared/_SalesVariantManager.cshtml`
   - `wwwroot/css/sales-variant-manager.css`
   - `wwwroot/js/sales-variant-manager.js`
2. Include CSS/JS in your layout:

```html
<link rel="stylesheet" href="~/css/sales-variant-manager.css" />
<script src="~/js/sales-variant-manager.js" defer></script>
```
3. Render the partial:

```cshtml
<partial name="_SalesVariantManager" />
```

## Included behavior

- Add/remove variant options
- Edit variant labels/descriptions
- Responsive two-column option editor
- Dynamic SKU matrix
- Price validation
- Stock/SKU inputs
- Per-variant image upload preview
- Bulk apply price/stock/SKU
- Size-chart template selector
- Collapsible matrix
- Exposes `window.SalesVariantManager.getData()` and `.setData(data)` for API integration

## Notes

The component is intentionally self-contained and does not require a UI framework.
