BufferPool: Provides high-performance buffer management for socket operations
=============================================================================

Background:
-----------
Buffers used by BeginSend and BeginReceive socket operations are pinned until the operation is complete. When there are many (thousands) of socket operations taking place silmutaneously, the GC cannot effectively compact memory due to many non-contiguous pinned memory locations.

Objective:
----------
BufferPool aims to reduce heap fragmentation by providing contiguous memory buffers for use by socket operations.
For more information see [Buffer Pooling for .NET Socket Operations].

Usage:
------

To create a buffer pool:

    BufferPool pool = new BufferPool(1 * 1024 * 1024, 1, 1);

Using the pool in synchronous send or receive socket operations:

```C#
    /** SENDING DATA **/
    // const int SEND_BUFFER_SIZE is the desired size of the send buffer in bytes 
    // byte[] data contains the data to be sent.
    
    using (var buffer = pool.GetBuffer(SEND_BUFFER_SIZE)) 
    { 
        buffer.FillWith(data); 
        socket.Send(buffer.GetSegments()); 
    }


    /** RECEIVING DATA **/

    // const int RECEIVE_BUFFER_SIZE is the desired size of the receive buffer in bytes 
    // byte[] data is where the received data will be stored.

    using (var buffer = pool.GetBuffer(RECEIVE_BUFFER_SIZE)) 
    { 
        socket.Receive(buffer.GetSegments()); 
        buffer.CopyTo(data); 
    }
```

Using the pool in asynchronous send and receive socket operations:

```C#
    /** SENDING DATA ASYNCHRONOUSLY **/
    
    // const int SEND_BUFFER_SIZE is the desired size of the send buffer in bytes 
    // byte[] data contains the data to be sent.
    
    var buffer = pool.GetBuffer(SEND_BUFFER_SIZE); 
    buffer.FillWith(data); 
    socket.BeginSend(buffer.GetSegments(), SocketFlags.None, SendCallback, buffer);
    
    //...
    
    
    //In the send callback.
    
    private void SendCallback(IAsyncResult ar) 
    { 
        var sendBuffer = (IBuffer)ar.AsyncState; 
        try 
        { 
            socket.EndSend(ar); 
        } 
        catch (Exception ex) 
        { 
            //Handle Exception here 
        } 
        finally 
        { 
            if (sendBuffer != null) 
            { 
                sendBuffer.Dispose(); 
            } 
        } 
    }
    
    /** RECEIVING DATA ASYNCHRONOUSLY **/
    
    // const int RECEIVE_BUFFER_SIZE is the desired size of the receive buffer in bytes. 
    // byte[] data is where the received data will be stored.
    
    var buffer = pool.GetBuffer(RECEIVE_BUFFER_SIZE); 
    socket.BeginReceive(buffer.GetSegments(), SocketFlags.None, ReadCallback, buffer);
    
    //...
    
    
    //In the read callback
    
    private void ReadCallback(IAsyncResult ar) 
    { 
        var recvBuffer = (IBuffer)ar.AsyncState; 
        int bytesRead = 0;
    
        try 
        { 
            bytesRead = socket.EndReceive(ar); 
            byte[] data = new byte[bytesRead &gt; 0 ? bytesRead : 0];
    
            if (bytesRead &gt; 0) 
            { 
                recvBuffer.CopyTo(data, 0, bytesRead);
    
                //Do anything else you wish with read data here. 
            } 
            else 
            { 
                return; 
            }
    
        } 
        catch (Exception ex) 
        { 
            //Handle Exception here 
        } 
        finally 
        { 
            if (recvBuffer != null) 
            { 
                recvBuffer.Dispose(); 
            } 
        }
    
        //Read/Expect more data                    
        var buffer = pool.GetBuffer(RECEIVE_BUFFER_SIZE); 
        socket.BeginReceive(buffer.GetSegments(), SocketFlags.None, ReadCallback, buffer);
    
    }
```

[Buffer Pooling for .NET Socket Operations]:http://ahuwanya.net/blog/post/Buffer-Pooling-for-NET-Socket-Operations.aspx