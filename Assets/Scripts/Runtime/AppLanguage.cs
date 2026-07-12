using System;
using UnityEngine;
using UnityEngine.UI;

public enum AppLanguageCode
{
    English,
    Filipino
}

public static class AppLanguage
{
    public const string PreferenceKey = "AguinaldoShrine.HomeLanguage";

    public static AppLanguageCode Current
    {
        get
        {
            string code = PlayerPrefs.GetString(PreferenceKey, "en");
            return string.Equals(code, "fil", StringComparison.OrdinalIgnoreCase)
                ? AppLanguageCode.Filipino
                : AppLanguageCode.English;
        }
    }

    public static bool IsFilipino => Current == AppLanguageCode.Filipino;

    public static void Set(AppLanguageCode language)
    {
        PlayerPrefs.SetString(PreferenceKey, language == AppLanguageCode.Filipino ? "fil" : "en");
        PlayerPrefs.Save();
    }

    public static string Text(string key)
    {
        if (IsFilipino)
        {
            switch (key)
            {
                case "status_ready": return "AR HANDA - SIMULAN";
                case "home_eyebrow": return "GABAY SA PAMANA GAMIT ANG AR";
                case "home_subtitle": return "Tuklasin ang kasaysayan gamit ang immersive AR navigation";
                case "language_label": return "Wika";
                case "live_ar_navigation": return "Live AR Navigation";
                case "live_ar_desc": return "Sundan ang real-time na gabay para libutin ang shrine";
                case "smart_scan": return "Smart Object Scan";
                case "smart_scan_desc": return "Awtomatikong makita ang artifacts at mga detalye";
                case "mini_map_guide": return "Gabay sa Mini Map";
                case "mini_map_desc": return "Subaybayan ang ruta gamit ang live indoor map";
                case "start_tour": return "Simulan ang Tour";
                case "gallery": return "Larawan";
                case "about": return "Tungkol";
                case "exit": return "Lumabas";
                case "about_body": return "Pinagsasama ng Digital Heritage Archive tour ang indoor navigation, image scanning, narration, at gallery archive sa isang mobile guide para sa mga bisita.";
                case "footer": return "Magsimula sa Main Hall para sa mas maayos na AR experience";
                case "scan_item": return "I-scan";
                case "back": return "< Balik";
                case "next": return "Susunod >";
                case "close": return "Isara";
                case "cancel": return "Kanselahin";
                case "previous": return "< Nakaraan";
                case "resume_scan": return "Ipagpatuloy Scan";
                case "qr_scan": return "QR Scan";
                case "qr_scanner": return "QR Scanner";
                case "pick_gallery": return "Pumili sa Larawan";
                case "replay_pause": return "Ulit / Pause";
                case "play_video": return "Play Video";
                case "video_lesson": return "Video Lesson";
                case "video_loading": return "Niloload ang video...";
                case "video_watch_hint": return "Panoorin ang video hanggang matapos. Lalabas ang short quiz pagkatapos.";
                case "video_error": return "Hindi ma-play ang video. Subukan ulit.";
                case "no_video_available": return "Walang video para sa item na ito.";
                case "short_quiz": return "Short Quiz";
                case "question": return "Tanong";
                case "score": return "Score mo";
                case "congratulations": return "Congratulations!";
                case "quiz_complete_body": return "Tapos na ang quiz. Pwede mo nang isara ito o balikan ang scan result.";
                case "done": return "Done";
                case "direction": return "Direksyon";
                case "mini_map": return "Mini Mapa";
                case "route_options": return "Opsyon sa Ruta";
                case "ground": return "Ground";
                case "first_floor": return "Unang Palapag";
                case "ground_floor": return "Ground Floor";
                case "basement": return "Basement";
                case "audio": return "TUNOG";
                case "loading": return "Inihahanda ang AR Tour";
                case "go_straight": return "Diretso";
                case "turn_right": return "Lumiko Pakanan";
                case "turn_left": return "Lumiko Pakaliwa";
                case "arrived": return "Nakarating Ka Na";
                case "proceed": return "Magpatuloy";
                case "tour_complete": return "Tapos na ang Tour";
                case "completed": return "Tapos";
                case "no_locations": return "Walang tour locations";
                case "add_locations": return "Magdagdag ng LocationTrigger objects at i-regenerate ang scene.";
                case "requesting_ar": return "Humihingi ng camera at AR services...";
                case "ar_camera_missing": return "Hindi nahanap ang AR camera. I-regenerate ang scene.";
                case "move_slowly": return "Dahan-dahang gumalaw at sundan ang blue route.";
                case "manual_advance": return "Lumipat sa susunod na lokasyon para sa testing.";
                case "installing_ar": return "Ini-install ang Google Play Services for AR...";
                case "unsupported_ar": return "Hindi supported ng device na ito ang ARCore.";
                case "tour_finished_status": return "Tapos na ang tour. Pindutin ang Lumabas para bumalik sa Home.";
                case "close_to_marker": return "Malapit ka na. Lumakad papunta sa marker.";
                case "straight_status": return "Diretso lang. Sundan ang blue route.";
                case "right_status": return "Lumiko pakanan at panatilihing kita ang marker.";
                case "left_status": return "Lumiko pakaliwa at panatilihing kita ang marker.";
                case "camera_off": return "Naka-off ang camera. Pindutin ang camera icon para ipagpatuloy ang AR scan.";
                case "camera_on": return "Naka-on ang camera. Ipinagpatuloy ang AR scan.";
                case "audio_muted": return "Naka-mute ang audio.";
                case "audio_unmuted": return "Naka-unmute ang audio.";
                case "no_scan_refs": return "Wala pang scan references sa scene na ito.";
                case "camera_hint_ready": return "Itutok ang camera sa nakarehistrong shrine image.";
                case "qr_hint_ready": return "Itutok ang camera sa QR marker ng item.";
                case "qr_hint_loading": return "Itutok ang camera sa QR marker. Handa na kapag tapos mag-load ang image tracking.";
                case "camera_hint_loading": return "Itutok ang camera sa shrine image. Handa na ang live scan habang naglo-load ang image tracking.";
                case "gallery_missing": return "Hindi naka-configure ang gallery picker sa scene na ito.";
                case "select_gallery": return "Pumili ng larawan mula sa phone gallery.";
                case "gallery_load_failed": return "Hindi ma-load ang napiling larawan.";
                case "no_gallery_match": return "Walang tugmang shrine image na nahanap.";
                case "tracking_unsupported": return "Hindi supported ng device na ito ang AR image tracking.";
                case "tracking_init_failed": return "Hindi ma-initialize ang runtime image tracking.";
                case "mutable_library_unsupported": return "Hindi supported ng device na ito ang mutable image libraries.";
                case "scan_paused": return "Naka-pause ang camera scanning.";
                case "scan_resumed": return "Ipinagpatuloy ang camera scanning.";
                case "still_scanning": return "Nag-scan pa rin. Lumapit sa larawan o subukan ang Gallery option.";
                case "source_live_scan": return "Auto-detected mula sa live camera scan.";
                case "source_qr_scan": return "Exact match mula sa QR scan.";
                case "source_live_frame": return "Auto-detected mula sa live camera frame.";
                case "source_gallery": return "Matched mula sa napiling gallery image.";
                case "gallery_empty_title": return "Walang archive photos";
                case "gallery_empty_body": return "Magdagdag ng images sa Assets/all pictures AR para lumabas sa app.";
                case "archive_header": return "Archive ng Larawan";
                case "archive_instruction": return "Pumili ng archive image sa ibaba para basahin ang in-app definition.";
                case "archive_all": return "Lahat ng Archive Photos";
                case "scan_choice_body": return "Mag-scan gamit ang live camera o pumili ng larawan mula sa gallery para makita ang impormasyon.";
                case "scan_choice_header": return "Smart scan ay aktibo";
                case "scan_active_hint": return "Aktibo ang smart scan. Itutok ang camera sa nakarehistrong shrine image.";
                case "scan_menu_body": return "Aktibo na ang smart scan habang naglilibot ka. Gamitin ang menu na ito para ipagpatuloy o pumili ng larawan.";
                case "scan_menu_body_qr": return "Gamitin ang QR Scan para sa pinakatumpak na item lookup, ipagpatuloy ang live scan, o pumili ng larawan.";
                case "no_item_matched": return "Wala pang item na tumugma";
                case "scanned_item": return "Na-scan na Heritage Item";
                case "archive_photo": return "Archive Photo";
                case "go_to_main_hall": return "Pumunta sa Main Hall";
                case "proceed_next_location": return "Magpatuloy sa susunod na lokasyon.";
            }
        }

        switch (key)
        {
            case "status_ready": return "AR READY - TAP TO BEGIN";
            case "home_eyebrow": return "AR HERITAGE EXPERIENCE";
            case "home_subtitle": return "Explore history through immersive augmented reality navigation";
            case "language_label": return "Language";
            case "live_ar_navigation": return "Live AR Navigation";
            case "live_ar_desc": return "Follow real-time arrows to explore the shrine";
            case "smart_scan": return "Smart Object Scan";
            case "smart_scan_desc": return "Automatically detect artifacts and view details";
            case "mini_map_guide": return "Mini Map Guide";
            case "mini_map_desc": return "Track your route with a live indoor map";
            case "start_tour": return "Start Tour";
            case "gallery": return "Gallery";
            case "about": return "About";
            case "exit": return "Exit";
            case "about_body": return "Digital Heritage Archive tour combines indoor navigation, image scanning, narration, and an archive gallery into one mobile guide for visitors.";
            case "footer": return "Start at the Main Hall for the best AR experience";
            case "scan_item": return "Scan Item";
            case "back": return "< Back";
            case "next": return "Next >";
            case "close": return "Close";
            case "cancel": return "Cancel";
            case "previous": return "< Previous";
            case "resume_scan": return "Resume Auto Scan";
            case "qr_scan": return "QR Scan";
            case "qr_scanner": return "QR Scanner";
            case "pick_gallery": return "Pick From Gallery";
            case "replay_pause": return "Replay / Pause";
            case "play_video": return "Play Video";
            case "video_lesson": return "Video Lesson";
            case "video_loading": return "Loading video...";
            case "video_watch_hint": return "Watch the video until it ends. A short quiz will appear after playback.";
            case "video_error": return "The video could not be played. Please try again.";
            case "no_video_available": return "No video is available for this item.";
            case "short_quiz": return "Short Quiz";
            case "question": return "Question";
            case "score": return "Score";
            case "congratulations": return "Congratulations!";
            case "quiz_complete_body": return "Quiz complete. You can close this and return to the scan result.";
            case "done": return "Done";
            case "direction": return "Direction";
            case "mini_map": return "Mini Map";
            case "route_options": return "Route Options";
            case "ground": return "Ground";
            case "first_floor": return "First Floor";
            case "ground_floor": return "Ground Floor";
            case "basement": return "Basement";
            case "audio": return "AUDIO";
            case "loading": return "Preparing AR Tour";
            case "go_straight": return "Go Straight";
            case "turn_right": return "Turn Right";
            case "turn_left": return "Turn Left";
            case "arrived": return "You have arrived";
            case "proceed": return "Proceed";
            case "tour_complete": return "Tour Complete";
            case "completed": return "Completed";
            case "no_locations": return "No tour locations found";
            case "add_locations": return "Add LocationTrigger objects and regenerate the scene.";
            case "requesting_ar": return "Requesting camera and AR services...";
            case "ar_camera_missing": return "AR camera not found. Please regenerate the scene.";
            case "move_slowly": return "Move slowly and follow the blue route.";
            case "manual_advance": return "Location advanced manually for testing.";
            case "installing_ar": return "Installing Google Play Services for AR...";
            case "unsupported_ar": return "ARCore is not supported on this Android device.";
            case "tour_finished_status": return "Tour finished. Tap Exit to return to the home screen.";
            case "close_to_marker": return "You're close. Walk to the marker.";
            case "straight_status": return "Straight ahead. Follow the blue route.";
            case "right_status": return "Turn right and keep the marker in view.";
            case "left_status": return "Turn left and keep the marker in view.";
            case "camera_off": return "Camera off. Tap the camera icon to resume AR scan.";
            case "camera_on": return "Camera on. AR scan resumed.";
            case "audio_muted": return "Audio muted.";
            case "audio_unmuted": return "Audio unmuted.";
            case "no_scan_refs": return "No scan references are available in this scene yet.";
            case "camera_hint_ready": return "Point the camera at a registered shrine image.";
            case "qr_hint_ready": return "Point the camera at the item's QR marker.";
            case "qr_hint_loading": return "Point the camera at the QR marker. It will be ready when image tracking finishes loading.";
            case "camera_hint_loading": return "Point the camera at a shrine image. Live scan is ready while image tracking finishes loading.";
            case "gallery_missing": return "Gallery picker is not configured in this scene.";
            case "select_gallery": return "Select an image from your phone gallery.";
            case "gallery_load_failed": return "The selected gallery image could not be loaded.";
            case "no_gallery_match": return "No matching shrine image was found.";
            case "tracking_unsupported": return "This device does not support AR image tracking.";
            case "tracking_init_failed": return "Runtime image tracking could not be initialized.";
            case "mutable_library_unsupported": return "This device does not support mutable image libraries.";
            case "scan_paused": return "Camera scanning paused.";
            case "scan_resumed": return "Camera scanning resumed.";
            case "still_scanning": return "Still scanning. Move closer to the image or try the Gallery option.";
            case "source_live_scan": return "Auto-detected from live camera scan.";
            case "source_qr_scan": return "Exact match from QR scan.";
            case "source_live_frame": return "Auto-detected from live camera frame.";
            case "source_gallery": return "Matched from selected gallery image.";
            case "gallery_empty_title": return "No archive photos found";
            case "gallery_empty_body": return "Add images to Assets/all pictures AR so they can appear inside the application.";
            case "archive_header": return "Aguinaldo Shrine Photo Archive";
            case "archive_instruction": return "Select any archive image below to read its in-app definition.";
            case "archive_all": return "All Archive Photos";
            case "scan_choice_body": return "Scan with the live camera or choose an image from the gallery to see a result.";
            case "scan_choice_header": return "Smart scan is active";
            case "scan_active_hint": return "Smart scan is active. Point the camera at a registered shrine image.";
            case "scan_menu_body": return "Smart scan is already active while you explore. Use this menu to resume or choose a gallery image.";
            case "scan_menu_body_qr": return "Use QR Scan for the most accurate item lookup, resume the live camera scan, or pick an image from the phone gallery.";
            case "no_item_matched": return "No item matched yet";
            case "scanned_item": return "Scanned Heritage Item";
            case "archive_photo": return "Archive Photo";
            case "go_to_main_hall": return "Go to Main Hall";
            case "proceed_next_location": return "Proceed to next location.";
            default: return key;
        }
    }

    public static string FormatDistance(float distanceMeters)
    {
        if (distanceMeters < 1f)
        {
            int centimeters = Mathf.RoundToInt(distanceMeters * 100f);
            return IsFilipino ? centimeters + " cm layo" : centimeters + " cm away";
        }

        string meters = distanceMeters.ToString("0.0");
        return IsFilipino ? meters + " m layo" : meters + " m away";
    }

    public static string FormatProgress(int current, int total)
    {
        return (IsFilipino ? "HINTO " : "STOP ") + current + " / " + total;
    }

    public static string ArrivedAt(string locationName)
    {
        return IsFilipino
            ? "Nakarating ka na sa " + locationName + "."
            : "Arrived at " + locationName + ".";
    }

    public static string RouteRewoundTo(string locationName)
    {
        return IsFilipino
            ? "Binalik ang ruta sa " + locationName + "."
            : "Route rewound to " + locationName + ".";
    }

    public static string LocalizeTourDescription(string locationName, string description)
    {
        if (!IsFilipino || string.IsNullOrWhiteSpace(description))
        {
            return description;
        }

        string normalized = Normalize(description);
        switch (normalized)
        {
            case "Starting point of the route. Stand here first so the AR guide can establish the tour direction.":
                return "Panimulang punto ng ruta. Tumayo muna rito para maayos ng AR guide ang direksyon ng tour.";
            case "Proceed to the Sala and follow the route as it opens into the main receiving area.":
                return "Pumunta sa Sala at sundan ang ruta papunta sa main receiving area.";
            case "Continue toward the Dining area where the household and ceremonial route continues.":
                return "Magpatuloy papunta sa Dining area kung saan nagpapatuloy ang ruta ng bahay at seremonya.";
            case "Move into the Bedroom stop and keep the arrow centered as the route narrows.":
                return "Pumunta sa Bedroom stop at panatilihing nasa gitna ang arrow habang kumikitid ang ruta.";
            case "Follow the guide to the Family Room and stay near the route line for the cleanest AR experience.":
                return "Sundan ang guide papunta sa Family Room at manatiling malapit sa route line para mas malinis ang AR experience.";
            case "Continue to the Secret Areas stop and let the guide lead you through the hidden route segment.":
                return "Magpatuloy sa Secret Areas stop at hayaan ang guide na dalhin ka sa nakatagong bahagi ng ruta.";
            case "Turn toward the War Memorabilia stop for the collection-focused portion of the tour.":
                return "Lumiko papunta sa War Memorabilia stop para sa bahagi ng tour na nakatuon sa collection.";
            case "Proceed to Documents and follow the mini-map as the route bends back across the gallery.":
                return "Pumunta sa Documents at sundan ang mini-map habang bumabalik ang ruta sa gallery.";
            case "Final destination. Complete the route at the Garden after reviewing the last heritage stop.":
                return "Huling destinasyon. Tapusin ang ruta sa Garden matapos makita ang huling heritage stop.";
            default:
                return description;
        }
    }

    public static string TranslateKnown(string text)
    {
        if (!IsFilipino || string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        string normalized = Normalize(text);
        switch (normalized)
        {
            case "AR READY - TAP TO BEGIN": return Text("status_ready");
            case "AR HERITAGE EXPERIENCE": return Text("home_eyebrow");
            case "Explore history through immersive augmented reality navigation": return Text("home_subtitle");
            case "Language": return Text("language_label");
            case "Live AR Navigation": return Text("live_ar_navigation");
            case "Follow real-time arrows to explore the shrine": return Text("live_ar_desc");
            case "Smart Object Scan": return Text("smart_scan");
            case "Automatically detect artifacts and view details": return Text("smart_scan_desc");
            case "Mini Map Guide": return Text("mini_map_guide");
            case "Track your route with a live indoor map": return Text("mini_map_desc");
            case "Start Tour": return Text("start_tour");
            case "Gallery": return Text("gallery");
            case "About": return Text("about");
            case "Exit": return Text("exit");
            case "Scan Item": return Text("scan_item");
            case "< Back": return Text("back");
            case "Back": return Text("back");
            case "Next >": return Text("next");
            case "Next": return Text("next");
            case "X Exit": return "X " + Text("exit");
            case "Close": return Text("close");
            case "Cancel": return Text("cancel");
            case "< Previous": return Text("previous");
            case "Previous": return Text("previous");
            case "Resume Auto Scan": return Text("resume_scan");
            case "QR Scan": return Text("qr_scan");
            case "QR Scanner": return Text("qr_scanner");
            case "Pick From Gallery": return Text("pick_gallery");
            case "Replay / Pause": return Text("replay_pause");
            case "Direction": return Text("direction");
            case "Mini Map": return Text("mini_map");
            case "Route Options": return Text("route_options");
            case "Ground": return Text("ground");
            case "First Floor": return Text("first_floor");
            case "Ground Floor": return Text("ground_floor");
            case "Basement": return Text("basement");
            case "AUDIO": return Text("audio");
            case "Preparing AR Tour": return Text("loading");
            case "Go Straight": return Text("go_straight");
            case "Turn Right": return Text("turn_right");
            case "Turn Left": return Text("turn_left");
            case "You have arrived": return Text("arrived");
            case "Proceed": return Text("proceed");
            case "Tour Complete": return Text("tour_complete");
            case "Completed": return Text("completed");
            case "Point the camera at a registered shrine image.": return Text("camera_hint_ready");
            case "Point the camera at the item's QR marker.": return Text("qr_hint_ready");
            case "Point the camera at the QR marker. It will be ready when image tracking finishes loading.": return Text("qr_hint_loading");
            case "Point the camera at a shrine image. Live scan is ready while image tracking finishes loading.": return Text("camera_hint_loading");
            case "Smart scan is active. Point the camera at a registered shrine image.": return Text("scan_active_hint");
            case "Waiting for an auto-detected match": return "Naghihintay ng auto-detected match";
            case "Aguinaldo Shrine Photo Archive": return Text("archive_header");
            case "Select any archive image below to read its in-app definition.": return Text("archive_instruction");
            case "All Archive Photos": return Text("archive_all");
            case "Scan with the live camera or choose an image from the gallery to see a result.": return Text("scan_choice_body");
            case "Smart scan is already active while you explore. Use this menu to resume": return Text("scan_menu_body");
            case "Smart scan is already active while you explore. Use this menu to resume or choose a gallery image.": return Text("scan_menu_body");
            case "Use QR Scan for the most accurate item lookup, resume the live camera scan, or pick an image from the phone gallery.": return Text("scan_menu_body_qr");
            case "No item matched yet": return Text("no_item_matched");
            case "Scanned Heritage Item": return Text("scanned_item");
            case "Archive Photo": return Text("archive_photo");
            case "Go to Main Hall": return Text("go_to_main_hall");
            case "Proceed to next location.": return Text("proceed_next_location");
            case "AR Scan Info System": return "Impormasyon ng AR Scan";
            default:
                if (normalized.StartsWith("STOP ", StringComparison.OrdinalIgnoreCase))
                {
                    return "HINTO " + normalized.Substring(5);
                }

                if (normalized.EndsWith(" m away", StringComparison.OrdinalIgnoreCase))
                {
                    return normalized.Substring(0, normalized.Length - " away".Length) + " layo";
                }

                return text;
        }
    }

    public static void ApplyToTextTree(Transform root)
    {
        if (root == null || !IsFilipino)
        {
            return;
        }

        Text[] texts = root.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null)
            {
                continue;
            }

            texts[i].text = TranslateKnown(texts[i].text);
        }
    }

    private static string Normalize(string text)
    {
        return string.Join(" ", text.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries));
    }
}
