## Manual Demo Downloader

Accepts manual uploads from the webapp and stores the files in blobstorage.

### Single operation
 If `POST`-Request is made to the `ManualDemoDownloadController`, the request first get checked for being a multipart request, before a temporary directory at 
`/tmp` is created.

The uploaded file is then checked for its extension. If it is an allowed type, then the file is uploaded to a blob in the storage account specefied by the 
`BLOB_CONNECTION_STRING`.



The blob's name is the original file name, and the container is `manual-upload`.

For example, if `test_demo2.dem.bz2` is uploaded, the blob could then be addressed at 
`http://<account-name>.<service-name>.core.windows.net/manual-upload/test_demo2.dem.bz2`.

After that a message is published to the `AMQP_UPLOAD_RECEIVED_QUEUE`.

### Environment Variables

- `AMQP_URI`[*]
- `AMQP_UPLOAD_RECEIVED_QUEUE`[*]
- `BLOB_CONNECTION_STRING ` [*]

[*] required


