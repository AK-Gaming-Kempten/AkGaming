using AkGaming.Gamenight.Contracts;
using Microsoft.AspNetCore.Components;
namespace AkGaming.Gamenight.Shared.Components;
public partial class GameNightFields
{
    [Parameter, EditorRequired] public SignupForm Form { get; set; } = default!;

    private void OrderChoiceChanged()
    {
        if (Form.WantsToOrder == false)
        {
            Form.DonerQuantity = 0;
            Form.PizzaQuantity = 0;
            Form.IceCreamQuantity = 0;
            Form.Meal = "Nichts";
            Form.MealQuantity = null;
            Form.IceCream = "Nein";
            Form.Scoops = null;
            return;
        }

        if (Form.WantsToOrder == true)
        {
            Form.DonerQuantity ??= Form.Meal is "Döner" or "Döner, Pizza" ? Form.MealQuantity ?? 0 : 0;
            Form.PizzaQuantity ??= Form.Meal is "Pizza" or "Döner, Pizza" ? Form.MealQuantity ?? 0 : 0;
            Form.IceCreamQuantity ??= Form.Scoops ?? 0;
        }
    }

    private void ChangeDoner(int amount) => Form.DonerQuantity = Math.Clamp((Form.DonerQuantity ?? 0) + amount, 0, 5);
    private void ChangePizza(int amount) => Form.PizzaQuantity = Math.Clamp((Form.PizzaQuantity ?? 0) + amount, 0, 5);

    private void ChangeIceCream(int amount)
    {
        Form.IceCreamQuantity = Math.Clamp((Form.IceCreamQuantity ?? 0) + amount, 0, 10);
        Form.Scoops = Form.IceCreamQuantity > 0 ? Form.IceCreamQuantity : null;
        if (Form.IceCreamQuantity > 0 && string.IsNullOrEmpty(Form.IceCream)) Form.IceCream = "Ja";
        if (Form.IceCreamQuantity == 0) Form.IceCream = "Nein";
    }
}
