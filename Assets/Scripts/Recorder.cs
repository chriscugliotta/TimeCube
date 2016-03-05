using UnityEngine;
using System.Collections;

/// <summary>
/// Animator Recorder script.
/// Should work we any animator, be sure to register your states in the dictionnary in InitStateDictionnary()
/// </summary>
public class Recorder : MonoBehaviour {
	
	Animator m_Animator;
	
	//public Texture Play;
	//public Texture Next;
	//public Texture Prev;
	//public Texture Pause;
	
	const int FrameCount  = 500;	
	public bool isRecording;
	
	float m_TimeLinePixelSize;
	
	const float buttonBorderWidth = 4;	
	System.Collections.Generic.List<int> samples = new System.Collections.Generic.List<int>();
	
	void Start () 
	{
		m_Animator = GetComponent<Animator>();			
		StartRecord();
	}
	
	void OnGUI() 
	{		
		if(isRecording)
		{
	        if (GUILayout.Button("Pause"))
			{
				StopRecord();	          
			}
		}
		else
		{
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Prev",GUILayout.ExpandWidth(false)))
			{
				m_Animator.playbackTime -= 0.03f;
			}
			if (GUILayout.Button("Play",GUILayout.ExpandWidth(false)))
			{
				StartRecord();
				return;
			}
			if (GUILayout.Button("Next",GUILayout.ExpandWidth(false)))
			{
				m_Animator.playbackTime += 0.03f;
			}
			GUILayout.EndHorizontal();
						
			
			m_TimeLinePixelSize = Screen.width -10;
			m_Animator.playbackTime = GUILayout.HorizontalSlider(m_Animator.playbackTime, m_Animator.recorderStartTime, m_Animator.recorderStopTime, GUILayout.Width(m_TimeLinePixelSize));			
		}		    
    }	
			
	private void StartRecord()
	{
		isRecording = true;
		samples.Clear();
		m_Animator.StopPlayback();
		m_Animator.StartRecording(FrameCount); // record a number of frame
	}
	
	private void StopRecord()
	{
		isRecording = false;
		m_Animator.StopRecording();
		m_Animator.StartPlayback();
		
	}

	void Update()
	{
		if(isRecording)
		{				
			if(samples.Count == (FrameCount-1)) // has looped, removed 1st sample
			{							
				samples.RemoveAt(0);							
			}
			samples.Add(m_Animator.GetCurrentAnimatorStateInfo(0).nameHash);			
		}			
	}
}
