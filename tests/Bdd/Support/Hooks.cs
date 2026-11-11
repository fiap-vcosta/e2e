using Reqnroll;
using Reqnroll.BoDi;

namespace E2e.Bdd.Support;

[Binding]
public sealed class Hooks
{
    private readonly IObjectContainer _container;

    public Hooks(IObjectContainer container)
    {
        _container = container;
    }

    [BeforeScenario]
    public void Register()
    {
        _container.RegisterInstanceAs(new JsonHttp());
        _container.RegisterInstanceAs(new SagaContext());
    }
}
