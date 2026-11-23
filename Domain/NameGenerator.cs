namespace Domain
{
    public static class NameGenerator
    {
        private static readonly string[] Adjectives =
        {
            "Sneaky", "Wobbly", "Sleepy", "Grumpy", "Fluffy", "Speedy", "Clumsy", "Sassy",
            "Jiggly", "Cheeky", "Dizzy", "Fancy", "Soggy", "Spicy", "Bouncy", "Frozen",
            "Giggly", "Hungry", "Lazy", "Mighty", "Noisy", "Shy", "Silly", "Wiggly"
        };

        private static readonly string[] Nouns =
        {
            "Potato", "Penguin", "Noodle", "Llama", "Pickle", "Waffle", "Sloth", "Banana",
            "Goose", "Taco", "Hamster", "Muffin", "Walrus", "Nugget", "Platypus", "Meatball",
            "Turtle", "Pancake", "Capybara", "Dumpling", "Raccoon", "Burrito", "Moose", "Snail"
        };

        public static string Generate()
        {
            string adjective = Adjectives[Random.Shared.Next(Adjectives.Length)];
            string noun = Nouns[Random.Shared.Next(Nouns.Length)];

            return $"{adjective} {noun}";
        }
    }
}
