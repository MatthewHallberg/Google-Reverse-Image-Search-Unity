using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.UI;

public class TakePicture : MonoBehaviour 
{
	//*****THE getData.php FILE THAT YOU NEED TO HOST ON A SERVER THAT HAS NODE.JS INSTALLED IS IN THE ASSETS FOLDER!*****

	//change this to reflect your custom hosted url followed by getData.php?url=
	private const string BASE_URL = "www.<YOUR_SITE>.com/getData.php?url=";
	//enter your google api key for custom search here
	private const string GOOGLE_API_KEY = "***************************";
	//enter your cloud name from cloudinary here
	private const string CLOUD_NAME = "*********";
	//enter your cloudinary upload preset name
	private const string UPLOAD_PRESET_NAME = "****";

	//private const string CLOUDINARY_API_KEY = "464228211727792";

	//private const string CLOUDINARY_SIGNATURE = "LUubXRT37rDkg8hI__AjyOEubWY";

	private const string IMAGE_SEARCH_URL = "https://www.google.com/searchbyimage?site=search&sa=X&image_url=";

	private const string GOOGLE_SEARCH_URL = "https://www.googleapis.com/customsearch/v1?key=" + GOOGLE_API_KEY +"&cref&q=";

	byte[] imageByteArray;

	public GameObject buttonObject;

	private string imageURl;

	private string imageIdentifier;

	private string timeStamp;

	private string wordsToSearch;

	private GameObject scanningObject;

	private GameObject line1Object;

	private GameObject line2Object;

	private Vector3 cameraForwardVector;
	private Vector3 cameraPosition;

	void Start(){

		buttonObject = GameObject.Find ("Button");
		scanningObject = GameObject.Find ("Image");
		line1Object = GameObject.Find ("line1");
		line2Object = GameObject.Find ("line2");

		//set default states of UI 
		scanningObject.SetActive (false);
		line1Object.SetActive (false);
		line1Object.transform.parent.gameObject.SetActive (false);
		line2Object.SetActive (false);
		line2Object.transform.parent.gameObject.SetActive (false);

	}

	/*this takes a screenshot, saves it to file, and reads the file to the variable imageByteArray
	 * I tried other methods of taking a picture like using Vuforia's Image class and also creating another
	 * Unity webcamtexture, none of which worked on both mobile and in the editor so this is what I landed on
	 */ 
	public IEnumerator TakePhoto()
	{
		string filePath;

			//on mobile platforms persistentDataPath is already prepended to file name when using CaptureScreenshot()
			if (Application.isMobilePlatform) {

				filePath = Application.persistentDataPath + "/image.png";
				Application.CaptureScreenshot ("/image.png");
				//must delay here so picture has time to save unfortunatly
				yield return new WaitForSeconds(1.5f);
				//Encode to a PNG
				imageByteArray = File.ReadAllBytes(filePath);

			} else {

				filePath = Application.dataPath + "/StreamingAssets/" + "image.png";
				Application.CaptureScreenshot (filePath);
				//must delay here so picture has time to save unfortunatly
				yield return new WaitForSeconds(1.5f);
				//Encode to a PNG
				imageByteArray = File.ReadAllBytes(filePath);
			}

		print ("photo done!!");
		StartCoroutine("UploadImage");

		buttonObject.SetActive (false);
		scanningObject.SetActive (true);

	}
	//uploads the image to Cloudinary (you must first create an unsigned upload preset for this to work) and gets the image url
	public IEnumerator UploadImage(){

		print ("uploading image...");
		string url = "https://api.cloudinary.com/v1_1/" + CLOUD_NAME + "/auto/upload/";

		WWWForm myForm = new WWWForm ();
		myForm.AddBinaryData ("file",imageByteArray);
		myForm.AddField ("upload_preset", UPLOAD_PRESET_NAME);

		WWW www = new WWW(url,myForm);
		yield return www;
		print (www.text);

		print ("done uploading!");

		//parse resulting string to get image url 
		imageURl = www.text.Split('"', '"')[41];
		print ("IMAGE URL: " + imageURl);

		/*I got burned out trying to figure out how to delete an image after we use it
		 * so if someone else could figure it out that would be great, you will probably
		 * need this image identifier and timestamp.
		 * imageIdentifier = www.text.Split('"', '"')[3]; 
		 * timeStamp = www.text.Split('"', '"')[25]; 
		 * print ("IMAGE Identifier: " + imageIdentifier);
		 * print ("TIMESTAMP: " + timeStamp);
		*/

		StartCoroutine ("reverseImageSearch");

	}

	/*this function passes the image url to the php script called getData.php that you must have hosted somewhere that node.js is also installed.
	 * The php will parse the line from the Google reverse image search that contains the best guess as to what the image is.
	 * This was the only way I could figure to follow the redirects from Google and also be able to see the HTML rendered from the Javascript.
	 * We can then take that line and parse it down further to get our wordsToSearch variable. 
	 */ 
	public IEnumerator reverseImageSearch(){

		//create the full search url by adding all 3 together
		string fullSearchURL = BASE_URL + WWW.EscapeURL(IMAGE_SEARCH_URL + imageURl);
		print (fullSearchURL);

		//create a new www object and pass in this search url
		WWW www = new WWW(fullSearchURL);
		yield return www;

		wordsToSearch = www.text.Substring(www.text.IndexOf(">")+1);

		print (wordsToSearch);	

		StartCoroutine ("GoogleSearchAPI");

	}

	//This does a custom google search for the wordsToSearch (google's best guess) 
	public IEnumerator GoogleSearchAPI(){

		string searchURL = GOOGLE_SEARCH_URL + WWW.EscapeURL (wordsToSearch);
		//send a new request to the google custom search API
		WWW www = new WWW(searchURL);
		yield return www;

		//THIS IS PROBABLY A BETTER JOB FOR WITH REGEX-but lets parse it like this so I don't have to explain regex or deserializing JSON in my video.

		//split string by lines
		var parsedData = www.text.Split('\n');

		print (www.text);

		//set default lines for the first result on google
		if (parsedData.Length > 42) {
			string line1 = parsedData [43];
			string line2 = parsedData [47];

			//lets check for wikipedia results and if there are any we will overwrite our default values
			for (int i = 0; i < parsedData.Length; i++) {

				if (parsedData [i].Contains ("Wikipedia")) {
					line1 = parsedData [i];
					line2 = parsedData [i + 4];
					break;
				}
			}

			//remove first unwanted characters from string
			line1 = line1.Remove(0,13);
			line2 = line2.Remove (0, 15);
			//remove last unwanted characters from string
			line1 = line1.Remove (line1.Length - 2);
			line2 = line2.Remove (line2.Length - 2);

			//remove new line characters from string we will add our own later.
			if (line2.Contains("\n")){
				line2.Replace("\n"," ");
			}

			CreateVisibleText (wordsToSearch, line1, line2);

		} else {

			string line1 = "ERROR";
			string line2 = "ERROR";

			CreateVisibleText (wordsToSearch, line1, line2);
		}
	
		scanningObject.SetActive (false);
		buttonObject.SetActive (true);

	}

	public void CreateVisibleText(string text1, string text2, string text3){

		//turn on both 3d text objects
		line1Object.SetActive (true);
		line2Object.SetActive (true);
		line1Object.transform.parent.gameObject.SetActive (true);
		line2Object.transform.parent.gameObject.SetActive (true);


		//replace the spaces in the best guess result from google with new lines (if there are any)
		if (text1.Contains(" ")){
			text1 = text1.Replace(" ","\n");
		}
		line1Object.GetComponent<Text> ().text = text1;

		//remove new line characters from text3
		if (text3.Contains("\\n")){
			text3 = text3.Replace(@"\n"," ");
		}
		//loop through all characters of the text and insert a new line after every third space.
		int spaceCounter = 0;
		for (int i = 0; i < text2.Length; i++) {

			if (text2[i] == ' '){
				spaceCounter++;
				if (spaceCounter % 3 == 0) {

					text2 = text2.Insert (i, "\n");
				}
			}
		}
		//insert new line after every fourth here
		spaceCounter = 0;
		for (int i = 0; i < text3.Length; i++) {

			if (text3[i] == ' '){
				spaceCounter++;
				if (spaceCounter % 4 == 0) {

					text3 = text3.Insert (i, "\n");
				}
			}
		}

		//I decided to not display the title of the webpage (text2) but you can add it here if you like. 
		line2Object.GetComponent<Text> ().text = text3;

	}

	//this is the function that gets called when the scan button is pressed.
	public void StartCamera(){
		//turn off text and backgrounds
		line1Object.SetActive (false);
		line2Object.SetActive (false);
		line1Object.transform.parent.gameObject.SetActive (false);
		line2Object.transform.parent.gameObject.SetActive (false);

		print ("button down....");
		//starts the process
		StartCoroutine ("TakePhoto");
	}
}