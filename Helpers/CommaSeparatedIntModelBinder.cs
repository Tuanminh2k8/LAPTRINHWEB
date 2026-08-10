using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Source.Helpers
{
    public class CommaSeparatedIntModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

            if (valueProviderResult == ValueProviderResult.None || string.IsNullOrWhiteSpace(valueProviderResult.FirstValue))
            {
                return Task.CompletedTask;
            }

            var rawValue = valueProviderResult.FirstValue;
            var values = new List<int>();

            foreach (var part in rawValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (int.TryParse(part, out int parsed))
                {
                    values.Add(parsed);
                }
            }

            if (values.Count == 0)
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    $"Giá trị '{rawValue}' không phải là danh sách số nguyên hợp lệ.");
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(values);
            return Task.CompletedTask;
        }
    }
}
