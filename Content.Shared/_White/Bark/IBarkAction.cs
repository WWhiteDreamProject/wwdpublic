namespace Content.Shared._White.Bark;


[ImplicitDataDefinitionForInheritors]
public partial interface IBarkAction
{
    public void Act(IDependencyCollection dependencyCollection, Entity<Components.BarkSourceComponent> entity, BarkData currentBark);
}
