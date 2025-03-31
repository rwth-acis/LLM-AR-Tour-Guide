//using Firebase.Storage;

namespace i5.LLM_AR_Tourguide.Evaluation
{
    /// <summary>
    ///     This class is used to upload files to Firebase Cloud Storage. Firebase is currently not used in the project.
    /// </summary>
    public class FirebaseCloudStorageUpload
    {
        public static void uploadString(string folder, string fileContent)
        {
            // If you want to collect data from the user, you can use foloowing code to access firebase or build an upload to your own server


            /*
            // Get a reference to the storage service, using the default Firebase App
            var storage = FirebaseStorage.DefaultInstance;

            // Create a storage reference from our storage service
            var storageRef =
                storage.GetReferenceFromUrl("ENTER STORAGE CONTAINER NAME HERE");

            var random = Random.Range(0, 1000000);
            // Combine folder with current date and time
            var filePath = folder + "/" + DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss") + random + ".txt";

            var testRef = storageRef.Child(filePath);

            var byteArray = Encoding.UTF8.GetBytes(fileContent);

            var newMetadata = new MetadataChange();
            newMetadata.ContentType = "text/plain";

            // Upload the file to the path
            testRef.PutBytesAsync(byteArray)
                .ContinueWith(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        DebugEditor.LogError(task.Exception.ToString());
                    }
                    else
                    {
                        // Metadata contains file metadata such as size, content-type, and md5hash.
                        var metadata = task.Result;
                        var md5Hash = metadata.Md5Hash;
                        DebugEditor.Log("Finished uploading...");
                        //DebugEditor.Log("md5 hash = " + md5Hash);
                    }
                });
                */
        }
    }
}