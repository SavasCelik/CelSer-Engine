# CelSerEngine

Info for implementing Pointer Scan:

Dark Byte:
>pointermap is not affected by max level. It's basically a snapshot of the game at that time.
> When you do a pointerscan and load a pointermap, CE will unpack that game snapshot and see how the memory looked like when you made it. With that information it can determine if the current pointer it has found will also be a valid result in the old snapshot.
> If not, then don't save it to disk, as it'd be useless anyhow. (See if like a rescan , but then not horribly slow)
> 
> As for finding 0 results, that depends on the max level and structsize of your actual pointerscan(so not the pointermap generation, but the scan where you make use of > the pointermaps), and of course of on the target process. (E.g emulators like java and webbrowsers are not going to give you anything useful)
 
 source: https://www.cheatengine.org/forum/viewtopic.php?p=5732952
