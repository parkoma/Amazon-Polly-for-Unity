using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Amazon.Polly;
using Amazon.Polly.Model;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.Runtime;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Net;

public class AWSPolly : MonoBehaviour {

    bool isProcess = false;
    public Dropdown Voice;
    public AmazonPollyClient client;
    CognitoAWSCredentials credentials;


    public string cognitoCredentials;
    public RegionEndpoint region = RegionEndpoint.APNortheast1;
    public string speech;
    public string voice;

    private string audio_path;

    void Awake()
    {
        credentials = new CognitoAWSCredentials(cognitoCredentials, RegionEndpoint.APNortheast1);  //CognitoCredential로 AWSCredential설정
    }

    public void ButtonEvent(InputField text)  //버튼 클릭 이벤트
    {
        PollyTTS(text);
    }

    public async void PollyTTS(InputField text)  //Polly 실행
    {
        if (!isProcess)
        {
            isProcess = true;  //처리 중일 동안에 Polly 조건 설정

            speech = text.text;  //InputField에 입력된 값 설정
            voice = Voice.options[Voice.value].text;  //목소리 설정

            client = new AmazonPollyClient(credentials, RegionEndpoint.APNortheast1);  //아마존 Polly 클라이언트 설정

            SynthesizeSpeechRequest synthesizeSpeechPresignRequest = new SynthesizeSpeechRequest();  //Polly 세부사항 설정
            synthesizeSpeechPresignRequest.Text = speech;
            synthesizeSpeechPresignRequest.VoiceId = voice;
            synthesizeSpeechPresignRequest.OutputFormat = OutputFormat.Ogg_vorbis;

            var audio = await client.SynthesizeSpeechAsync(synthesizeSpeechPresignRequest);  //Polly 결과값 받아오기

            audio_path = Application.persistentDataPath + "/" + (int)(System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1))).TotalSeconds + ".mp3";  //오디오 패스 설정
            using (FileStream fileStream = File.Create(audio_path))
            {
                CopyTo(audio.AudioStream, fileStream);  //오디오 패스에 mp3 저장
            }

            StartCoroutine(PlayAudioClip(audio_path));  //오디오 패스에 있는 오디오 실행
            text.text = "";  //InputField 초기화
        }

    }

    IEnumerator PlayAudioClip(string audio_path)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + audio_path, AudioType.OGGVORBIS))  //오디오 클립 가져오기
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError)
                print(www.error);
            else
            {
                AudioClip audio = DownloadHandlerAudioClip.GetContent(www);
                GetComponent<AudioSource>().PlayOneShot(audio);  //오디오 클립 실행
                yield return new WaitForSeconds(audio.length + 0.1f);  //실행중에 코루틴 일시정지
                File.Delete(audio_path);  //오디오 실행된 후에 오디오 파일 삭제
            }
        }

        isProcess = false;  //처리 종료
    }

    public static void CopyTo(Stream input, Stream outputAudio)  //오디오 파일 저장
    {
        byte[] buffer = new byte[16 * 1024];
        int bytesRead;

        while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
        {
            outputAudio.Write(buffer, 0, bytesRead);
        }
    }

}
