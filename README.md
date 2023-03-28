# CelSerEngine

## Info for implementing Pointer Scan:

Dark Byte:
>pointermap is not affected by max level. It's basically a snapshot of the game at that time.
> When you do a pointerscan and load a pointermap, CE will unpack that game snapshot and see how the memory looked like when you made it. With that information it can determine if the current pointer it has found will also be a valid result in the old snapshot.
> If not, then don't save it to disk, as it'd be useless anyhow. (See if like a rescan , but then not horribly slow)
> 
> As for finding 0 results, that depends on the max level and structsize of your actual pointerscan(so not the pointermap generation, but the scan where you make use of > the pointermaps), and of course of on the target process. (E.g emulators like java and webbrowsers are not going to give you anything useful)
 
 source: https://www.cheatengine.org/forum/viewtopic.php?p=5732952

### Q&A
dbsxdbsx:
> To found base address of a game, "pointer scan " seems to be a cool way to do it.
> But I want to know more detail about it. Frankly, I want to make it my own way in C++.
>
> Question 1: How does CE decides whether a certain address is a base address? I guess is that CE would verify it by checking if an address is in the region of a certain module(main exe module or other dll module)?
>
> Question 2:For a more lazy way to use pointer scan, it is suggested that first generate a pointer map A after getting value with a dynamic address, then restart game, getting value with another dynamic address and generate pointer map B. Finally, do "pointer scan" with this address while loading B and compare A.
> But I don't get the meaning of the 2 maps. For map B, since "point scan" is on the way, why generating map first? Just for use after another restarting game?
> And especially for map A, what does "compare" mean here? In my opinion, the thing to be compared should not be the address to search for, as the game is restarted, then does it mean comparing "path to search for"---if this is the case, why path would be different every time game restarted?
> 
> Question3: This maybe a related question for the above question--- we know sometimes a base address may not be for a real static or global variable, maybe it just a quite static address allocated in stack. Therefore, sometimes we could see the very 1st base address is like "xxx.exe - offset" with a negative offset. But how does CE decides how much deep to search this negative offsets.

Dark Byte:

> 1: yes, it checks if it's indide a module or stack base (depending on options)
> 
> 2: both map A and map B will of course have different addresses
> when you have loaded a map you have to give it the address at the time map was made (the pointerscan config window has a dropdown list of memoryrecord addresses at the time the map was generated)
> That way when CE finds a pointer with the information of map A it can then check if that same pointerpath also points to the address of map B
> 
> 3: that is determined by the maximum stacksize to be deemed as static in the pointerscan config window (bottom right of advanced options)

source: https://cheatengine.org/forum/viewtopic.php?t=610818
