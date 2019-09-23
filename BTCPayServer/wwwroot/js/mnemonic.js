
var jQuerymnemonicsElement = "#phrase";
var btnGenerateMnemonic = "#generateMnemonic";
var mnemonics = { "english": new Mnemonic("english") };
var mnemonic = mnemonics["english"];

$(document).ready(function () {

    $(jQuerymnemonicsElement)
        .focusout(function () {
            validatePhrase();
        })
        /* .blur(function() {
           validatePhrase();
         })*/
        ;

    /*$(jQuerymnemonicsElement).change(function(){
       validatePhrase();
    }); */


    $(btnGenerateMnemonic).click(function (e) {
        e.preventDefault();
        // mnemonics is populated as required by getLanguage
        var mnemonic = mnemonics["english"];

        var numWords = parseInt(12);
        var strength = numWords / 3 * 32;
        var buffer = new Uint8Array(strength / 8);
        // create secure entropy
        var data = crypto.getRandomValues(buffer);
        // show the words
        var words = mnemonic.toMnemonic(data);
        var errorText = findPhraseErrors(words);
        if (errorText) {
            feedbackShow(errorText);
        } else {
            $(jQuerymnemonicsElement).val(words);
        }
    });
});

function validatePhrase() {
    var errorText = findPhraseErrors(getPhrase());
    if (errorText) {
        feedbackShow(errorText);
    }
}

function getPhrase() {
    return $(jQuerymnemonicsElement).val();
}

function feedbackShow(msg) {
    alert(msg);
}

function findPhraseErrors(phrase) {
    // Preprocess the words
    phrase = mnemonic.normalizeString(phrase);
    var words = phraseToWordArray(phrase);
    // Detect blank phrase
    if (words.length == 0) {
        return "Blank mnemonic";
    }
    // Check each word
    for (var i = 0; i < words.length; i++) {
        var word = words[i];
        var language = getLanguage();
        if (WORDLISTS[language].indexOf(word) == -1) {
            var nearestWord = findNearestWord(word);
            return word + " not in wordlist, did you mean " + nearestWord + "?";
        }
    }
    // Check the words are valid
    var properPhrase = wordArrayToPhrase(words);
    var isValid = mnemonic.check(properPhrase);
    if (!isValid) {
        return "Invalid mnemonic";
    }
    return false;
}

// TODO look at jsbip39 - mnemonic.splitWords
function phraseToWordArray(phrase) {
    var words = phrase.split(/\s/g);
    var noBlanks = [];
    for (var i = 0; i < words.length; i++) {
        var word = words[i];
        if (word.length > 0) {
            noBlanks.push(word);
        }
    }
    return noBlanks;
}

// TODO look at jsbip39 - mnemonic.joinWords
function wordArrayToPhrase(words) {
    var phrase = words.join(" ");
    var language = getLanguageFromPhrase(phrase);
    if (language == "japanese") {
        phrase = words.join("\u3000");
    }
    return phrase;
}

function findNearestWord(word) {
    var language = getLanguage();
    var words = WORDLISTS[language];
    var minDistance = 99;
    var closestWord = words[0];
    for (var i = 0; i < words.length; i++) {
        var comparedTo = words[i];
        if (comparedTo.indexOf(word) == 0) {
            return comparedTo;
        }
        var distance = Levenshtein.get(word, comparedTo);
        if (distance < minDistance) {
            closestWord = comparedTo;
            minDistance = distance;
        }
    }
    return closestWord;
}

function getLanguage() {
    var defaultLanguage = "english";
    // Try to get from existing phrase
    var language = getLanguageFromPhrase();
    // Try to get from url if not from phrase
    if (language.length == 0) {
        language = getLanguageFromUrl();
    }
    // Default to English if no other option
    if (language.length == 0) {
        language = defaultLanguage;
    }
    return language;
}

function getLanguageFromPhrase(phrase) {
    // Check if how many words from existing phrase match a language.
    var language = "";
    if (!phrase) {
        phrase = getPhrase();
    }
    if (phrase.length > 0) {
        var words = phraseToWordArray(phrase);
        var languageMatches = {};
        for (l in WORDLISTS) {
            // Track how many words match in this language
            languageMatches[l] = 0;
            for (var i = 0; i < words.length; i++) {
                var wordInLanguage = WORDLISTS[l].indexOf(words[i]) > -1;
                if (wordInLanguage) {
                    languageMatches[l]++;
                }
            }
            // Find languages with most word matches.
            // This is made difficult due to commonalities between Chinese
            // simplified vs traditional.
            var mostMatches = 0;
            var mostMatchedLanguages = [];
            for (var l in languageMatches) {
                var numMatches = languageMatches[l];
                if (numMatches > mostMatches) {
                    mostMatches = numMatches;
                    mostMatchedLanguages = [l];
                }
                else if (numMatches == mostMatches) {
                    mostMatchedLanguages.push(l);
                }
            }
        }
        if (mostMatchedLanguages.length > 0) {
            // Use first language and warn if multiple detected
            language = mostMatchedLanguages[0];
            if (mostMatchedLanguages.length > 1) {
                console.warn("Multiple possible languages");
                console.warn(mostMatchedLanguages);
            }
        }
    }
    return language;
}

function getLanguageFromUrl() {
    for (var language in WORDLISTS) {
        if (window.location.hash.indexOf(language) > -1) {
            return language;
        }
    }
    return "";
}
