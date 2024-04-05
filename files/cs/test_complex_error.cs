using System;

public interface IJacK<out T> {
    void Do(T inp);
}
