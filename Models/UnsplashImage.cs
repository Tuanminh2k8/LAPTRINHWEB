using System.Collections.Generic;
using System.Linq;

namespace Source.Models
{
    /// <summary>
    /// Sinh URL ảnh Unsplash (thật, đã verify tồn tại) phù hợp với tên món.
    /// Dùng pool theo từng chủng loại + seed (id món) để mỗi món có ảnh riêng, đa dạng.
    /// </summary>
    public static class UnsplashImage
    {
        private static readonly Dictionary<string, string[]> Pools = new()
        {
            ["burger"] = new[]
            {
                "1568901346375-23c9450c58cd",
                "1550547660-d9450f859349",
                "1561758033-d89a9ad46330",
                "1572802419224-296b0aeee0d9",
                "1586190848861-99aa4a171e90",
                "1607013251379-e6eecfffe234",
                "1565299507177-b0ac66763828",
                "1610440042657-612c34d95e9f"
            },
            ["pizza"] = new[]
            {
                "1513104890138-7c749659a591",
                "1565299624946-b28f40a0ae38",
                "1593504049359-74330189a345"
            },
            ["chicken"] = new[]
            {
                "1562967914-608f82629710",
                "1626645738196-c2a7c87a8f58"
            },
            ["side"] = new[]
            {
                "1541599468348-e96984315921",
                "1573080496219-bb080dd4f877",
                "1630383249896-424e482df921"
            },
            ["drink"] = new[]
            {
                "1509042239860-f550ce710b93",
                "1461023058943-07fcbe16d735",
                "1513558161293-cdaf765ed2fd",
                "1571934811356-5cc061b6821f",
                "1542990253-a781e04c0082",
                "1437418747212-8d9709afab22"
            },
            ["dessert"] = new[]
            {
                "1567206563064-6f60f40a2b57",
                "1497034825429-c343d7c6a68f",
                "1557142046-c704a3adf364",
                "1551024601-bec78aea704b"
            },
            ["salad"] = new[]
            {
                "1540420773420-3366772f4999",
                "1512621776951-a57141f2eefd",
                "1551248429-40975aa4de74",
                "1490645935967-10de6ba17061",
                "1535914254981-b5012eebbd15"
            },
            ["breakfast"] = new[]
            {
                "1490474418585-ba9bad8fd0ea",
                "1525351484163-7529414344d8",
                "1551218808-94e220e084d2",
                "1533089860892-a7c6f0a88666",
                "1528735602780-2552d3a8b7d0"
            },
            // Ảnh food chung (dùng khi không khớp từ khóa nào)
            ["default"] = new[]
            {
                "1568901346375-23c9450c58cd",
                "1513104890138-7c749659a591",
                "1562967914-608f82629710"
            }
        };

        private static readonly string[] SizeParams = { "?w=600&q=80&auto=format&fit=crop" };

        /// <summary>Từ keyword (burger/pizza/chicken/side/drink/dessert/salad/breakfast/food/cola/fries...).</summary>
        public static string ForKeyword(string keyword, int seed)
        {
            var key = (keyword ?? "food").ToLowerInvariant() switch
            {
                "burger" => "burger",
                "pizza" => "pizza",
                "friedchicken" or "chicken" => "chicken",
                "fries" or "side" => "side",
                "drink" or "cola" => "drink",
                "dessert" => "dessert",
                "salad" => "salad",
                "breakfast" => "breakfast",
                _ => "default"
            };
            return Build(key, seed);
        }

        /// <summary>Từ tên món (tiếng Việt) → keyword → pool.</summary>
        public static string For(string name, int seed)
        {
            var n = (name ?? "").ToLowerInvariant();
            string key = "default";

            if (n.Contains("pizza")) key = "pizza";
            else if (n.Contains("burger")) key = "burger";
            else if (n.Contains("gà") || n.Contains("ga")) key = "chicken";
            else if (n.Contains("salad") || n.Contains("wrap")) key = "salad";
            else if (n.Contains("sáng") || n.Contains("ngũ cốc") || n.Contains("mì") || n.Contains("ốp la") || n.Contains("trứng")) key = "breakfast";
            else if (n.Contains("khoai") || n.Contains("hành tây") || n.Contains("phô mai que") || n.Contains("bánh mì") || n.Contains("súp") || n.Contains("chả giò") || n.Contains("bánh bao") || n.Contains("vòng")) key = "side";
            else if (n.Contains("kem") || n.Contains("bánh") || n.Contains("pudding") || n.Contains("chè") || n.Contains("flan") || n.Contains("crepe") || n.Contains("donut") || n.Contains("brownie") || n.Contains("tart") || n.Contains("trái cây") || n.Contains("chuối") || n.Contains("xôi") || n.Contains("sữa chua") || n.Contains("cheesecake")) key = "dessert";
            else if (n.Contains("cà phê") || n.Contains("cafe") || n.Contains("trà") || n.Contains("sữa") || n.Contains("nước") || n.Contains("pepsi") || n.Contains("soda") || n.Contains("bia") || n.Contains("sting") || n.Contains("monster") || n.Contains("milo") || n.Contains("sinh tố") || n.Contains("cam") || n.Contains("chanh") || n.Contains("táo") || n.Contains("matcha") || n.Contains("suối") || n.Contains("latte") || n.Contains("coca")) key = "drink";

            return Build(key, seed);
        }

        private static string Build(string key, int seed)
        {
            var pool = Pools.TryGetValue(key, out var p) && p.Length > 0 ? p : Pools["default"];
            var idx = Math.Abs(seed) % pool.Length;
            return "https://images.unsplash.com/photo-" + pool[idx] + SizeParams[0];
        }
    }
}
