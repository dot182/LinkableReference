# LinkableReference
Instead of needing this:
``` csharp
public class Sample : MonoBehaviour
{
    [SerializeReference] public SomeData ObjectA; 
    [SerializeReference] public SomeData ObjectB;
    void OnValidate()
    {
      ObjectB = ObjectA;
    }
}
```
LinkableReference turns it into this:
``` csharp
public class Sample : MonoBehaviour
{
    public SomeData ObjectA; 
    [SerializeReference, LinkableReference] public SomeData ObjectB;
}
```
![img](https://github.com/user-attachments/assets/d9dfd5f6-4cc0-4f16-a2a3-97d8aff92bb1)

- Works with lists/arrays
- Tested on Unity 6

## Installation
1. Open Package Manager
2. “Add package from git URL…”
3. Input:
```
https://github.com/dot182/LinkableReference.git
```
## Limitations
- Objects have to be on the same unity object
- Only works on reference types
