# SRTReader
Simple srt reader

简单的一个SRT读取工具

# Quickly Start
```C#
var ins = new SRTReader();
ins.Load(<your srt string>);//you can call Load() for reusing SRTReader
```

```C#
TimeSpan time = <your movie current time>;
string subtitle = ins.GetByTime(time);
```

# References  
The code use BoysheO.Buffers.PooledBuffer and Collections.Pooled ,but you can replace them easliy.Remove IDisposable,use List instead of PooledList and use ToList() instead of ToPooledListBuffer().
