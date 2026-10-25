using System;

/// <summary>所有ドメインが計算した不変の結果と、その値を確定する操作。</summary>
internal interface IPreparedChange
{
    void Validate();
    void Commit();
}

/// <summary>
/// 変更結果を一度だけ計算・確保し、同じ値を状態更新と週報に使用する。
/// 確定操作には所有ドメイン内の代入のみを渡す。新たな計算や外部呼出しは含めない。
/// シミュレーションは単一スレッドで、全変更をValidateしてから連続してCommitする。
/// </summary>
internal sealed class PreparedChange<T> : IPreparedChange
{
    private readonly Func<bool> _isCurrent;
    private readonly Action _apply;
    private bool _committed;

    internal T Result { get; }

    internal PreparedChange(T result, Func<bool> isCurrent, Action apply)
    {
        Result = result;
        _isCurrent = isCurrent;
        _apply = apply;
    }

    public void Validate()
    {
        if (_committed || !_isCurrent())
            throw new InvalidOperationException("確定済み、または計算後に変更された状態には適用できません。");
    }

    internal T Commit()
    {
        Validate();
        _apply();
        _committed = true;
        return Result;
    }

    void IPreparedChange.Commit() => Commit();
}
