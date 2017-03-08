<?php

#access by <YourSiteName.com>/getData.php?url=<DesiredUrl>

#DesiredURL is https://www.google.com/searchbyimage?&image_url=<YOUR IMAGE URL>

#grab desired url from url 
$url = $_GET["url"];
#save the html from the url as a variable
$htmlResult = open_url($url);
#get the desired text from between two strings
$parsed = get_string_between($htmlResult, 'class="_gUb"', '</a>');
#echo the parsed result as html to the page
echo $parsed; 

	#this function returns what is in between two strings
	function get_string_between($string, $start, $end){
		$string = ' ' . $string;
		$ini = strpos($string, $start);
		if ($ini == 0) return '';
		$ini += strlen($start);
		$len = strpos($string, $end, $ini) - $ini;
		return substr($string, $ini, $len);
	}

	#this function takes in a url and uses cUrl to follow the redirect chain and output the desired html
	#credit for this goes to Artur Schaback
    function open_url($full_url){
        $curl = curl_init();
        curl_setopt($curl, CURLOPT_URL, $full_url);
        curl_setopt($curl, CURLOPT_HEADER, 0);
        curl_setopt($curl, CURLOPT_RETURNTRANSFER, 1);
        curl_setopt($curl, CURLOPT_REFERER, 'http://localhost');
        curl_setopt($curl, CURLOPT_SSL_VERIFYPEER, false);
        curl_setopt($curl, CURLOPT_USERAGENT, "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.11 (KHTML, like Gecko) Chrome/23.0.1271.97 Safari/537.11");
        curl_setopt($curl, CURLOPT_FOLLOWLOCATION, true);
        $content = utf8_decode(curl_exec($curl));
        curl_close($curl);
        return $content;
    }


?>