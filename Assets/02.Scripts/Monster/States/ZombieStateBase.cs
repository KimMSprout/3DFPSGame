// 좀비 상태 하나가 가져야 하는 인터페이스
public interface IZombieState
{
    void Enter();
    void Update();
    void Exit();
}

// 모든 좀비 상태의 공통 부모. 좀비 참조와 빈 Enter/Exit를 제공한다.
public abstract class ZombieStateBase : IZombieState
{
    protected readonly Zombie _zombie;

    protected ZombieStateBase(Zombie zombie)
    {
        _zombie = zombie;
    }

    public virtual void Enter() { }
    public abstract void Update();
    public virtual void Exit() { }
}
