Provides buffer management for asynchronous socket operations
=============================================================

Background:
-----------
Buffers used by BeginSend and BeginReceive socket operations are pinned until the operation is complete. When there are many (thousands) of socket operations taking place silmutaneously, the GC cannot effectively compact memory due to many non-contiguous pinned memory locations.

Objective:
----------
BufferPool aims to reduce heap fragmentation by providing contiguous memory buffers for use by socket operations. 