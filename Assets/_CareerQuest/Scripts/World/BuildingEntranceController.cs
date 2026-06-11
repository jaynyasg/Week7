namespace CareerQuest
{
    internal sealed class BuildingEntranceController
    {
        private readonly CampusWorldBuilder _builder;

        public BuildingEntranceController(CampusWorldBuilder builder)
        {
            _builder = builder;
        }

        public void AddHubDecor()
        {
            _builder.AddBuilding("Design Build Studio", -3.0f, 0.8f, 2.15f, 1.55f, CampusWorldPalette.Coral, CampusWorldPalette.CoralRoof, 4);
            _builder.AddBuilding("Health Hero Clinic", 0f, 1f, 2.1f, 1.45f, CampusWorldPalette.Mint, CampusWorldPalette.TealRoof, 4);
            _builder.AddBuilding("Logic Court", 3.0f, 0.8f, 2.15f, 1.55f, CampusWorldPalette.Gold, CampusWorldPalette.GoldRoof, 4);

            _builder.AddSmallBuilding("AI Lab", -4.45f, -1.75f, CampusWorldPalette.SkyBlue);
            _builder.AddSmallBuilding("Music Studio", -2.05f, -2f, CampusWorldPalette.Lilac);
            _builder.AddSmallBuilding("Robotics", 2.05f, -2f, CampusWorldPalette.Teal);
            _builder.AddSmallBuilding("Kitchen", 4.45f, -1.75f, CampusWorldPalette.Leaf);

            _builder.AddTree(-4.8f, 1f);
            _builder.AddTree(4.8f, 1.1f);
            _builder.AddTree(-4.8f, -0.65f);
            _builder.AddTree(4.8f, -0.55f);
        }
    }
}
