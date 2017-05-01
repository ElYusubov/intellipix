# Summary

## In this hands-on lab, you learned how to:

- Create an Azure storage account and use it as a backing store for an app
- Create a Web app in Visual Studio, test it locally, and deploy it to Azure
- Write code that uploads blobs to blob storage and attaches metadata to them
- Consume blob metadata to implement search
- Use Microsoft's Computer Vision API to generate metadata from images
- There is much more that you could do to develop Intellipix and to leverage Azure even further. For example, you could add support for authenticating users and deleting photos, and rather than force the user to wait for Cognitive Services to process a photo following an upload, you could use Azure Functions to call the Computer Vision API asynchronously each time an image is added to blob storage. You could even use
- Cognitive Services to detect faces in the photos and analyze the emotions depicted by those faces. With the cloud as your platform, the sky is the limit (pun intended).