using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenCVForUnity;

public class AuthScript : MonoBehaviour
{

	WebCamTexture webCamTexture;

	WebCamDevice webCamDevice;

	Color32[] colors;

	public bool isFrontFacing = false;

	public GameObject sphere1, sphere2, sphere3, sphere4;

	//Standard height and width of webcam input
	static int width = 480;
	static int height = 640;

	//Number of Skin trackers
	static int NSAMPLES = 7;

	//Opencv Mat
	public static Mat frame, frame_pot, frame_hsv, frame_thresh_final, frameclone, frame_thresh_show;
	public static Mat[] frame_thresh;

	Scalar[] lowerBound = new Scalar[NSAMPLES], upperBound = new Scalar[NSAMPLES];

	//Texture
	public static Texture2D texture;

	bool initDone = false;
	static bool cap = true, initiate = true, proc = false, play = false, auth = false, correct = false, skipFrames = false;

	int counter = 0;

	String input = "", password = "1234";
	Char previous = '0';

	int square_len = 12, n;
	List<My_ROI> roi;

	int[,] avgColor = new int[NSAMPLES, 3];
	int[,] c_lower = new int[NSAMPLES, 3];
	int[,] c_upper = new int[NSAMPLES, 3];

	double angle_rot;
	float posX_new;
	float posY_new;

	//Variable for GUI
	public float minSwipeDistY;
	public float minSwipeDistX;
	private Vector2 startPos;
	float touchx, touchy;
	public float Speed = 10.0f;

	List<Point> sList = new List<Point>(), eList = new List<Point>();

	void Start ()
	{
		StartCoroutine (init ());
	}

	private IEnumerator init ()
	{
		if (webCamTexture != null) {
			webCamTexture.Stop ();
			initDone = false;
			frame.Dispose ();
		}

		// Checks how many and which cameras are available on the device
		for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++) {
				
				if (WebCamTexture.devices [cameraIndex].isFrontFacing == isFrontFacing) {
					
					Debug.Log (cameraIndex + " name " + WebCamTexture.devices [cameraIndex].name + " isFrontFacing " + WebCamTexture.devices [cameraIndex].isFrontFacing);

					webCamDevice = WebCamTexture.devices [cameraIndex];

					webCamTexture = new WebCamTexture (webCamDevice.name, width, height);

					break;

				}
		}
			
		if (webCamTexture == null) {
			webCamDevice = WebCamTexture.devices [0];
			webCamTexture = new WebCamTexture (webCamDevice.name, width, height);
		}
			
		Debug.Log ("width " + webCamTexture.width + " height " + webCamTexture.height + " fps " + webCamTexture.requestedFPS);
			
			
			
		// Starts the camera
		webCamTexture.Play ();


		while (true) {
			//If you want to use webcamTexture.width and webcamTexture.height on iOS, you have to wait until webcamTexture.didUpdateThisFrame == 1, otherwise these two values will be equal to 16. (http://forum.unity3d.com/threads/webcamtexture-and-error-0x0502.123922/)
			#if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
//		   	if (webCamTexture.width > 16 && webCamTexture.height > 16) {
			#else
			if (webCamTexture.didUpdateThisFrame) {
				#endif
				Debug.Log ("width " + webCamTexture.width + " height " + webCamTexture.height + " fps " + webCamTexture.requestedFPS);
				Debug.Log ("videoRotationAngle " + webCamTexture.videoRotationAngle + " videoVerticallyMirrored " + webCamTexture.videoVerticallyMirrored + " isFrongFacing " + webCamDevice.isFrontFacing);
					
				colors = new Color32[webCamTexture.width * webCamTexture.height];
					
				frame = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC3);
				frame_pot = new Mat (webCamTexture.width, webCamTexture.height, CvType.CV_8UC3);
				frame_hsv = new Mat (webCamTexture.width, webCamTexture.height, CvType.CV_8UC3);
				//final_thresh = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC1);
				frame_thresh = new Mat[NSAMPLES];
				frame_thresh[0] = new Mat(webCamTexture.width, webCamTexture.height, CvType.CV_8UC1);
				frame_thresh[1] = new Mat(webCamTexture.width, webCamTexture.height, CvType.CV_8UC1);
				frame_thresh[2] = new Mat(webCamTexture.width, webCamTexture.height, CvType.CV_8UC1);
				frame_thresh[3] = new Mat(webCamTexture.width, webCamTexture.height, CvType.CV_8UC1);
				frame_thresh[4] = new Mat(webCamTexture.width, webCamTexture.height, CvType.CV_8UC1);
				frame_thresh[5] = new Mat(webCamTexture.width, webCamTexture.height, CvType.CV_8UC1);
				frame_thresh[6] = new Mat(webCamTexture.width, webCamTexture.height, CvType.CV_8UC1);

				texture = new Texture2D (webCamTexture.width, webCamTexture.height, TextureFormat.RGB24, false);

				roi = new List<My_ROI>();

				c_lower [0,0] = 12; //12
				c_upper [0,0] = 7;  //7
				c_lower [0,1] = 30; //30
				c_upper [0,1] = 40; //40
				c_lower [0,2] = 80; //80
				c_upper [0,2] = 80; //80

				gameObject.transform.eulerAngles = new Vector3 (0, 0, 0);
				#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
				gameObject.transform.eulerAngles = new Vector3 (0, 0, -90);
				#endif

				gameObject.transform.localScale = new Vector3 (webCamTexture.width, webCamTexture.height, 1);

				gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

				#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
				Camera.main.orthographicSize = webCamTexture.width / 2;
				#else
				Camera.main.orthographicSize = webCamTexture.height / 2;
				#endif

				initDone = true;
				
				break;
			} else {
				yield return 0;
			}
		}
	}
	
	void Update () {
		if (!initDone)
			return;

		if (webCamTexture.didUpdateThisFrame) {
						
			if (cap) {
				auth = false;
				TextoMat();
				if(frame != null){
					frame_pot = frame.t();
					Core.flip (frame_pot, frame_pot, 1);

					Imgproc.cvtColor(frame_pot, frame_hsv, Imgproc.COLOR_BGR2HSV);
					sphere1.SetActive(false);
					sphere2.SetActive(false);
					sphere3.SetActive(false);
					sphere4.SetActive(false);
					WaitForPalmCover ();

					Core.flip (frame_pot, frame_pot, 1);
					frame = frame_pot.t();
					MattoTex();
				}
			} else if (initiate) {
				TextoMat();
				if(frame != null){
					initiate = false;

					frame_pot = frame.t();
					Core.flip (frame_pot, frame_pot, 1);

					WaitForPalmCover();
					proc = true;
				}
			}

			if (proc) {
				Average ();
				ProduceBinaries ();

				Core.flip (frame_pot, frame_pot, 1);
				frame = frame_pot.t();

				MattoTex();
				proc = false;
				
				play = true;
				//StartCoroutine(WaitAndDoSomething());

				Material material1 = sphere1.GetComponent<Renderer>().material;
				Material material2 = sphere2.GetComponent<Renderer>().material;
				Material material3 = sphere3.GetComponent<Renderer>().material;
				Material material4 = sphere4.GetComponent<Renderer>().material;
				material1.color = Color.cyan;
				material2.color = Color.cyan;
				material3.color = Color.cyan;
				material4.color = Color.cyan;
			} 	

			if (play) {
				TextoMat();
				if(frame != null) {
					sphere1.SetActive(true);
					sphere2.SetActive(true);
					sphere3.SetActive(true);
					sphere4.SetActive(true);
					frame_pot = frame.t();
					Core.flip (frame_pot, frame_pot, 1);

					if (!auth) {
						Imgproc.cvtColor(frame_pot, frame_hsv, Imgproc.COLOR_BGR2HSV);
						PerformThresholding();
						Process();
						DrawLines();
					} else {
						if (correct) {
							string imText = "AUTHENTICATED";
							Core.putText(frame_pot, imText, new Point(105, 50), Core.FONT_HERSHEY_COMPLEX, 1.0, new Scalar(0, 255, 0), 2);	
							Material material1 = sphere1.GetComponent<Renderer>().material;
							Material material2 = sphere2.GetComponent<Renderer>().material;
							Material material3 = sphere3.GetComponent<Renderer>().material;
							Material material4 = sphere4.GetComponent<Renderer>().material;
							material1.color = Color.green;
							material2.color = Color.green;
							material3.color = Color.green;
							material4.color = Color.green;
						} else {
							string imText = "FAILED";
							Core.putText(frame_pot, imText, new Point(180, 50), Core.FONT_HERSHEY_COMPLEX, 1.0, new Scalar(255, 0, 0), 2);	
							Material material1 = sphere1.GetComponent<Renderer>().material;
							Material material2 = sphere2.GetComponent<Renderer>().material;
							Material material3 = sphere3.GetComponent<Renderer>().material;
							Material material4 = sphere4.GetComponent<Renderer>().material;
							material1.color = Color.red;
							material2.color = Color.red;
							material3.color = Color.red;
							material4.color = Color.red;
						}
					}

					//frame_pot = frame_thresh_final.clone();
					Core.flip (frame_pot, frame_pot, 1);
					//frame_thresh_show = frame_pot.t();
					frame = frame_pot.t();
					MattoTex();
				}
			}
		}
	}

	IEnumerator WaitAndDoSomething() {
		TextoMat();
		if(frame != null){
			MattoTex();
		}
		yield return new WaitForSeconds(2f);
		play = true;
	}

	void DrawLines() {
		for (int i = 0; i < sList.Count; i++) {
			Core.line(frame_pot, sList.ElementAt(i), eList.ElementAt(i), new Scalar(255, 255, 0), 10);
		}
	}

	void TextoMat () {
		Utils.webCamTextureToMat (webCamTexture, frame);

		if (webCamTexture.videoVerticallyMirrored) {
			if (webCamDevice.isFrontFacing) {
				if (webCamTexture.videoRotationAngle == 0) {
					Core.flip (frame, frame, 1);
				} else if (webCamTexture.videoRotationAngle == 90) {
					Core.flip (frame, frame, 0);
				} else if (webCamTexture.videoRotationAngle == 270) {
					Core.flip (frame, frame, 1);
				}
			} else {
				if (webCamTexture.videoRotationAngle == 90) {
					
				} else if (webCamTexture.videoRotationAngle == 270) {
					Core.flip (frame, frame, -1);
				}
			}
		} else {
			if (webCamDevice.isFrontFacing) {
				if (webCamTexture.videoRotationAngle == 0) {
					Core.flip (frame, frame, 1);
				} else if (webCamTexture.videoRotationAngle == 90) {
					Core.flip (frame, frame, 0);
				} else if (webCamTexture.videoRotationAngle == 270) {
					Core.flip (frame, frame, 1);
				}
			} else {
				if (webCamTexture.videoRotationAngle == 90) {
					
				} else if (webCamTexture.videoRotationAngle == 270) {
					Core.flip (frame, frame, -1);
				}
			}
		}
	}

	void MattoTex() {
		Utils.matToTexture2D (frame, texture);
		gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
	}

	void MattoTexThresh() {
		Utils.matToTexture2D (frame_thresh_show, texture);
		gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
	}

	void WaitForPalmCover() {
		
		My_ROI roi1 = new My_ROI (width / 2 - 3 * square_len, height / 2 + 12 * square_len, square_len);
		My_ROI roi2 = new My_ROI (width / 2 + 3 * square_len, height / 2 + 12 * square_len, square_len);
		My_ROI roi3 = new My_ROI (width / 2 - 3 * square_len, height / 2 + 18 * square_len, square_len); 
		My_ROI roi4 = new My_ROI (width / 2 + 3 * square_len, height / 2 + 18 * square_len, square_len);
		My_ROI roi5 = new My_ROI (width / 2 + 1 * square_len, height / 3 + 10 * square_len, square_len);
		My_ROI roi6 = new My_ROI (width / 2 - 2 * square_len, height / 3 + 10 * square_len, square_len);
		My_ROI roi7 = new My_ROI (width / 2, height / 2 + 15 * square_len, square_len);
		
		roi.Add (roi1);
		roi.Add (roi2);
		roi.Add (roi3);
		roi.Add (roi4);
		roi.Add (roi5);
		roi.Add (roi6);
		roi.Add (roi7);

		if (initiate) {
			for (int i=0; i<NSAMPLES; i++) {
				roi[i].draw_rectangle();
			}
			roi.Clear ();
			string imText = "COVER TRACKERS";
			Core.putText(frame_pot, imText, new Point(100, 100), Core.FONT_HERSHEY_COMPLEX, 1.0, new Scalar(0, 255, 0), 2);		
		}
	}

	void Average() {
		for(int i=0; i<NSAMPLES; i++)
			GetAvgColor(i);
	}

	void GetAvgColor(int t) {
		Mat r_hsv = new Mat ();
		r_hsv = roi [t].roi_ptr.clone ();
		List<int> hm = new List<int>();
		List<int> sm = new List<int>();
		List<int> lm = new List<int>();
		byte[] data = new byte[3];
		for (int i=2; i<(r_hsv.height() - 2); i++) {
			for (int j=2; j<(r_hsv.width() - 2); j++) {
				r_hsv.get(i, j, data);
				hm.Add((int)data[0]);
				sm.Add((int)data[1]);
				lm.Add((int)data[2]);
			}
		}
		avgColor[t, 0] = GetMedian (hm);
		avgColor[t, 1] = GetMedian (sm);
		avgColor[t, 2] = GetMedian (lm);
	}


	int GetMedian(List<int> val ) {
		int median;
		int size = val.Count;
		val.Sort ();
		if (size % 2 == 0)
			median = val [size/2];
		else
			median = val [size/2 + 1];
		return median;
	}

	void normalizeColors() {
		for(int i=1;i<NSAMPLES;i++){
			for(int j=0;j<3;j++){
				c_lower[i,j]=c_lower[0,j];	
				c_upper[i,j]=c_upper[0,j];	
			}	
		}
		
		for(int i=0;i<NSAMPLES;i++){
			if((avgColor[i,0]-c_lower[i,0]) <0){
				c_lower[i,0] = avgColor[i,0] ;
			}if((avgColor[i,1]-c_lower[i,1]) <0){
				c_lower[i,1] = avgColor[i,1] ;
			}if((avgColor[i,2]-c_lower[i,2]) <0){
				c_lower[i,2] = avgColor[i,2] ;
			}if((avgColor[i,0]+c_upper[i,0]) >255){ 
				c_upper[i,0] = 255-avgColor[i,0] ;
			}if((avgColor[i,1]+c_upper[i,1]) >255){
				c_upper[i,1] = 255-avgColor[i,1] ;
			}if((avgColor[i,2]+c_upper[i,2]) >255){
				c_upper[i,2] = 255-avgColor[i,2] ;
			}
		}
	}

	void ProduceBinaries() {
	//	Scalar lowerBound, upperBound;
		normalizeColors ();
		for (int i=0; i<NSAMPLES; i++) {
			lowerBound[i] = new Scalar (avgColor [i,0] - c_lower [i,0], avgColor [i,1] - c_lower [i,1], avgColor [i,2] - c_lower [i,2]);
			upperBound[i] = new Scalar (avgColor [i,0] + c_upper [i,0], avgColor [i,1] + c_upper [i,1], avgColor [i,2] + c_upper [i,2]);
		}
	}

	void PerformThresholding() {
		for (int i=0; i<NSAMPLES; i++) {
			Core.inRange(frame_hsv, lowerBound[i], upperBound[i], frame_thresh[i]);
		}
		frame_thresh_final = frame_thresh[0];
		for (int i=1; i<NSAMPLES; i++) {
			Core.add(frame_thresh_final, frame_thresh[i], frame_thresh_final); 
		}

		Imgproc.medianBlur(frame_thresh_final, frame_thresh_final, 1);
		Imgproc.medianBlur(frame_thresh_final, frame_thresh_final, 3);
		Imgproc.medianBlur(frame_thresh_final, frame_thresh_final, 5);
		Imgproc.medianBlur(frame_thresh_final, frame_thresh_final, 7);
		Imgproc.medianBlur(frame_thresh_final, frame_thresh_final, 9);
	}

	void Process() {
		string imText = "DRAW PATTERN";
		Core.putText(frame_pot, imText, new Point(110, 50), Core.FONT_HERSHEY_COMPLEX, 1.0, new Scalar(255, 0, 0), 2);	

		Mat hierarchy = new Mat ();
		List<MatOfPoint> contours = new List<MatOfPoint> ();
		MatOfPoint maxitem = new MatOfPoint ();
		MatOfInt hullInt = new MatOfInt ();
		
		frameclone = frame_thresh_final.clone ();
		Imgproc.findContours (frameclone, contours, hierarchy, Imgproc.RETR_LIST , Imgproc.CHAIN_APPROX_NONE);
		
		maxitem = contours [0];
		n = 0;
		for(int i=0; i<contours.Count; i++){
			if(contours[i].total() > maxitem.total()){
				maxitem = contours[i];
				n=i;
			}
		}
		
		OpenCVForUnity.Rect bRect = Imgproc.boundingRect (maxitem);
		int bRect_height = bRect.height;
		int bRect_width = bRect.width;
		if (bRect_height < 200 || bRect_width < 200)
			return;
		
		// Drawing Contours on the Frame
		//Imgproc.drawContours (frame_pot, contours, n, new Scalar(0, 255, 0), 2);
		
		Imgproc.convexHull (maxitem, hullInt);

		List<Point> maxitemPointList = maxitem.toList ();
		List<int> hullIntList = hullInt.toList ();
		List<Point> hullPointList = new List<Point> ();
		
		for (int j=0; j < hullInt.toList().Count; j++) {
			hullPointList.Add (maxitemPointList [hullIntList [j]]);
		}
		
		MatOfPoint hullPointMat = new MatOfPoint ();
		hullPointMat.fromList (hullPointList);

		List<MatOfPoint> hullPoints = new List<MatOfPoint> ();
		hullPoints.Add (hullPointMat);
		
		// Drawing Convex Hull on the Frame
		//Imgproc.drawContours (frame_pot, hullPoints, -1, new Scalar (0, 0, 255), 2);
		
		MatOfInt4 convexityDef = new MatOfInt4 ();
		Imgproc.convexityDefects (maxitem, hullInt, convexityDef);
		
		List<int> conDefIntList = convexityDef.toList ();
		List<Point> startpts = new List<Point> ();
		List<Point> farpts = new List<Point> ();
		List<Point> endpts = new List<Point> ();
		
		int tolerance = (int)(bRect_height/6);
		//Debug.Log ("Tolerance: " + tolerance);
		int[] defarray = new int[100];

		int coordX = 10000, coordY = 10000;

		int x1 = (int) sphere1.transform.position.x; 
		int y1 = (int) sphere1.transform.position.y;
		int x2 = (int) sphere2.transform.position.x;
		int y2 = (int) sphere2.transform.position.y;
		int x3 = (int) sphere3.transform.position.x; 
		int y3 = (int) sphere3.transform.position.y;
		int x4 = (int) sphere4.transform.position.x; 
		int y4 = (int) sphere4.transform.position.y;		

		Point pointer = new Point();

		for(int i=0; i < conDefIntList.Count/4 ; i++) {
			startpts.Add(maxitemPointList[conDefIntList[4*i]]);
			endpts.Add(maxitemPointList[conDefIntList[4*i+1]]);
			farpts.Add(maxitemPointList[conDefIntList[4*i+2]]);
			
			Point s = startpts[i];
			Point e = endpts[i];
			Point f = farpts[i];

			if (GetDistance(s,f) > tolerance) {
				//Core.circle(frame_pot, s, 15, new Scalar(255, 225, 0), -1);
				if (s.y < coordY) {
					pointer = s;
					coordY = (int) s.y;
					coordX = (int) s.x;
				}
			}
		}

		Core.circle(frame_pot, pointer, 15, new Scalar(255, 225, 0), -1);

		coordX = coordX - 240;
		coordY = -coordY + 320;

		if (coordX > x1-50 && coordX < x1+50 && coordY > y1-50 && coordY < y1+50) {
			if (previous.Equals('1'))
				return;
			input += "1";
			AddLine(previous, '1');
			previous = '1';
			Material mat1 = sphere1.GetComponent<Renderer>().material;
			mat1.color = Color.yellow;
			StartCoroutine(WaitAndChangeColor("1"));
		} else if (coordX > x2-50 && coordX < x2+50 && coordY > y2-50 && coordY < y2+50) {
			if (previous.Equals('2'))
				return;
			input += "2";
			AddLine(previous, '2');
			previous = '2';
			Material mat2 = sphere2.GetComponent<Renderer>().material;
			mat2.color = Color.yellow;
			StartCoroutine(WaitAndChangeColor("2"));
		} else if (coordX > x3-50 && coordX < x3+50 && coordY > y3-50 && coordY < y3+50) {
			if (previous.Equals('3'))
				return;
			input += "3";
			AddLine(previous, '3');
			previous = '3';
			Material mat3 = sphere3.GetComponent<Renderer>().material;
			mat3.color = Color.yellow;
			StartCoroutine(WaitAndChangeColor("3"));
		} else if (coordX > x4-50 && coordX < x4+50 && coordY > y4-50 && coordY < y4+50) {
			if (previous.Equals('4'))
				return;
			input += "4";
			AddLine(previous, '4');
			previous = '4';
			Material mat4 = sphere4.GetComponent<Renderer>().material;
			mat4.color = Color.yellow;
			StartCoroutine(WaitAndChangeColor("4"));
		}

		if (input.Length == password.Length) {
			auth = true;
			if (input.Equals(password)) {
				correct = true;
			} else {
				correct = false;
			}
		}
	}

	IEnumerator WaitAndChangeColor(String num) {
		yield return new WaitForSeconds(0.2f);
		if (!auth) {
			if (num.Equals("1")) {
				Material mat1 = sphere1.GetComponent<Renderer>().material;
				mat1.color = Color.cyan;
			} else if (num.Equals("2")) {
				Material mat2 = sphere2.GetComponent<Renderer>().material;
				mat2.color = Color.cyan;
			} else if (num.Equals("3")) {
				Material mat3 = sphere3.GetComponent<Renderer>().material;
				mat3.color = Color.cyan;
			} else if (num.Equals("4")) {
				Material mat4 = sphere4.GetComponent<Renderer>().material;
				mat4.color = Color.cyan;
			}
		}
	}

	void AddLine(Char prev, Char cur) {
		if (prev.Equals('0'))
			return;
		Point sPoint = getPoint(prev);
		Point ePoint = getPoint(cur);
		sList.Add(sPoint);
		eList.Add(ePoint);
		Core.line(frame_pot, sPoint, ePoint, new Scalar(255, 255, 0), 4);
	}

	Point getPoint(Char x) {
		int x1 = (int) sphere1.transform.position.x; 
		int y1 = (int) sphere1.transform.position.y;
		int x2 = (int) sphere2.transform.position.x;
		int y2 = (int) sphere2.transform.position.y;
		int x3 = (int) sphere3.transform.position.x; 
		int y3 = (int) sphere3.transform.position.y;
		int x4 = (int) sphere4.transform.position.x; 
		int y4 = (int) sphere4.transform.position.y;

		x1 = x1 + 240;
		y1 = 320 - y1;
		x2 = x2 + 240;
		y2 = 320 - y2;
		x3 = x3 + 240;
		y3 = 320 - y3;
		x4 = x4 + 240;
		y4 = 320 - y4;	

		if (x.Equals('1'))
			return new Point(x1, y1);
		else if (x.Equals('2'))
			return new Point(x2, y2);
		else if (x.Equals('3'))
			return new Point(x3, y3);
		else
			return new Point(x4, y4);
	}

	private double GetAngle(Point s, Point f, Point e) {
		double l1 = GetDistance(f, s);
		double l2 = GetDistance(f, e);
		double dot = (s.x-f.x)*(e.x-f.x) + (s.y-f.y)*(e.y-f.y);
		double angle = Math.Acos(dot/(l1*l2));
		angle = angle * 180 / Math.PI;
		return angle;
	}
	
	private double GetDistance(Point p1, Point p2) {
		return Math.Sqrt(Math.Pow(p1.x - p2.x, 2) + Math.Pow(p1.y - p2.y, 2));
	}
	
	void OnDisable () {
		webCamTexture.Stop ();
	}
	
	void OnGUI () {
		minSwipeDistX = (float)(Screen.width / 4);
		minSwipeDistY = (float)(Screen.height / 4);


		#if (UNITY_ANDROID || UNITY_IOS)
		//Android
		int touchCount = Input.touchCount;
		#endif

		GUIStyle myButtonStyle = new GUIStyle(GUI.skin.button);
		myButtonStyle.fontSize = 80;


		if (cap) {
			if (GUI.Button(new UnityEngine.Rect(0.0f,(float)(7*Screen.height/8), Screen.width, (float)(Screen.height/8)), "Capture", myButtonStyle)) {
				cap = false;
			}
		} else {
			if( GUI.Button(new UnityEngine.Rect(0.0f,(float)(7*Screen.height/8), Screen.width, (float)(Screen.height/8)), "Reset", myButtonStyle)){
				cap = true;
				initiate = true;
				proc = false;
				play = false;
				auth = false;
				correct = false;
				input = "";
				previous = '0';
				sList = new List<Point>(); 
				eList = new List<Point>();
			}
		}
	}

	public class My_ROI{
		public Mat roi_ptr;
		Size size;
		int cornerx, cornery, sqlen;
		
		public My_ROI(int u_corner_x, int u_corner_y, int sq_len){
			cornerx = u_corner_x;
			cornery = u_corner_y;
			sqlen = sq_len;
			roi_ptr = frame_hsv.submat(cornery, cornery + sqlen, cornerx, cornerx + sqlen);
		}
		
		public void draw_rectangle(){
			Core.rectangle (frame_pot, new Point (cornerx, cornery), new Point (cornerx + sqlen, cornery + sqlen), new Scalar(0, 255, 0), 2);
		}
	}
}
