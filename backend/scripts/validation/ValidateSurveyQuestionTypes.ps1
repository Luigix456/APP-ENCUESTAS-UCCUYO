param(
    [string]$BaseUrl = "http://localhost:5047",
    [string]$AdminEmail = "admin@institucion.edu.ar"
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$baseUrlPrompt = Read-Host "BaseUrl [$BaseUrl]"
if (-not [string]::IsNullOrWhiteSpace($baseUrlPrompt)) {
    $BaseUrl = $baseUrlPrompt
}

$adminEmailPrompt = Read-Host "Admin email [$AdminEmail]"
if (-not [string]::IsNullOrWhiteSpace($adminEmailPrompt)) {
    $AdminEmail = $adminEmailPrompt
}

$script:BaseUrl = $BaseUrl.TrimEnd("/")
$script:CurrentStep = "Initializing"

function Write-Step {
    param([string]$Message)

    $script:CurrentStep = $Message
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Fail {
    param(
        [string]$Message,
        [object]$Expected = $null,
        [object]$Actual = $null,
        [string]$ResponseBody = $null
    )

    Write-Host ""
    Write-Host "VALIDATION FAILED" -ForegroundColor Red
    Write-Host "Step: $script:CurrentStep" -ForegroundColor Red
    Write-Host "Message: $Message" -ForegroundColor Red

    if ($null -ne $Expected) {
        Write-Host "Expected: $Expected" -ForegroundColor Yellow
    }

    if ($null -ne $Actual) {
        Write-Host "Actual: $Actual" -ForegroundColor Yellow
    }

    if (-not [string]::IsNullOrWhiteSpace($ResponseBody)) {
        Write-Host "Response body:" -ForegroundColor Yellow
        Write-Host $ResponseBody
    }

    exit 1
}

function Convert-SecureStringToPlainText {
    param([System.Security.SecureString]$SecureString)

    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureString)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

function Convert-FromJsonSafe {
    param([string]$Content)

    if ([string]::IsNullOrWhiteSpace($Content)) {
        return $null
    }

    try {
        return $Content | ConvertFrom-Json
    }
    catch {
        return $null
    }
}

function Read-ErrorResponseBody {
    param([object]$Exception)

    if ($null -eq $Exception.Response) {
        return ""
    }

    if ($Exception.Response.PSObject.Methods.Name -contains "GetResponseStream") {
        $stream = $Exception.Response.GetResponseStream()
        if ($null -eq $stream) {
            return ""
        }

        $reader = New-Object System.IO.StreamReader($stream)
        try {
            return $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }

    return ""
}

function Get-ErrorStatusCode {
    param([object]$Exception)

    if ($null -eq $Exception.Response) {
        return 0
    }

    $statusCodeProperty = $Exception.Response.PSObject.Properties["StatusCode"]
    if ($null -ne $statusCodeProperty -and $null -ne $statusCodeProperty.Value) {
        return [int]$statusCodeProperty.Value
    }

    return 0
}

function Invoke-Api {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Path,
        [object]$Body = $null,
        [string]$Token = $null,
        [int[]]$ExpectedStatus = @(200)
    )

    $uri = "$script:BaseUrl$Path"
    $headers = @{}

    if (-not [string]::IsNullOrWhiteSpace($Token)) {
        $headers["Authorization"] = "Bearer $Token"
    }

    $parameters = @{
        Uri = $uri
        Method = $Method
        Headers = $headers
        UseBasicParsing = $true
        ErrorAction = "Stop"
    }

    if ($null -ne $Body) {
        $parameters["ContentType"] = "application/json"
        $parameters["Body"] = ($Body | ConvertTo-Json -Depth 30)
    }

    try {
        $response = Invoke-WebRequest @parameters
        $statusCode = [int]$response.StatusCode
        $rawBody = [string]$response.Content
        $parsedBody = Convert-FromJsonSafe $rawBody
    }
    catch {
        $statusCode = Get-ErrorStatusCode $_.Exception
        $rawBody = ""

        if ($null -ne $_.ErrorDetails -and -not [string]::IsNullOrWhiteSpace($_.ErrorDetails.Message)) {
            $rawBody = $_.ErrorDetails.Message
        }

        if ([string]::IsNullOrWhiteSpace($rawBody)) {
            $rawBody = Read-ErrorResponseBody $_.Exception
        }

        $parsedBody = Convert-FromJsonSafe $rawBody

        if ($ExpectedStatus -notcontains $statusCode) {
            Fail "Unexpected HTTP status calling $Method $Path" ($ExpectedStatus -join ", ") $statusCode $rawBody
        }

        return [pscustomobject]@{
            StatusCode = $statusCode
            Body = $parsedBody
            RawBody = $rawBody
        }
    }

    if ($ExpectedStatus -notcontains $statusCode) {
        Fail "Unexpected HTTP status calling $Method $Path" ($ExpectedStatus -join ", ") $statusCode $rawBody
    }

    return [pscustomobject]@{
        StatusCode = $statusCode
        Body = $parsedBody
        RawBody = $rawBody
    }
}

function Assert-Equal {
    param(
        [object]$Expected,
        [object]$Actual,
        [string]$Message
    )

    if ($Expected -ne $Actual) {
        Fail $Message $Expected $Actual
    }
}

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        Fail $Message "true" "false"
    }
}

function Assert-ActiveResource {
    param(
        [object]$Resource,
        [string]$Name
    )

    if ($null -ne $Resource.PSObject.Properties["isActive"] -and $Resource.isActive -ne $true) {
        Fail "$Name exists but is not active." "isActive = true" $Resource.isActive
    }
}

function Assert-DecimalApprox {
    param(
        [decimal]$Expected,
        [object]$Actual,
        [string]$Message,
        [decimal]$Tolerance = 0.01
    )

    $actualDecimal = [decimal]$Actual
    $difference = [Math]::Abs($actualDecimal - $Expected)

    if ($difference -gt $Tolerance) {
        Fail $Message $Expected $actualDecimal
    }
}

function Assert-ContainsValue {
    param(
        [object[]]$Values,
        [object]$Expected,
        [string]$Message
    )

    if ($Values -notcontains $Expected) {
        Fail $Message $Expected ($Values -join ", ")
    }
}

function Assert-NotContainsValue {
    param(
        [object[]]$Values,
        [object]$Unexpected,
        [string]$Message
    )

    if ($Values -contains $Unexpected) {
        Fail $Message "value not present" $Unexpected
    }
}

function New-Answer {
    param(
        [Guid]$QuestionId,
        [Guid[]]$OptionIds = @(),
        [string]$TextValue = $null,
        [int]$NumericValue = $null,
        [object[]]$MatrixAnswers = @(),
        [string]$Comment = $null,
        [string]$OtherText = $null
    )

    $answer = @{
        questionId = $QuestionId
        optionIds = @($OptionIds)
        textValue = $TextValue
        numericValue = $null
        comment = $Comment
        matrixAnswers = @($MatrixAnswers)
        otherText = $OtherText
    }

    if ($PSBoundParameters.ContainsKey("NumericValue")) {
        $answer.numericValue = $NumericValue
    }

    return $answer
}

function Submit-Response {
    param(
        [string]$AccessCode,
        [object[]]$Answers,
        [int[]]$ExpectedStatus = @(201)
    )

    return Invoke-Api `
        -Method "POST" `
        -Path "/api/public/survey-sessions/$AccessCode/responses" `
        -Body @{ answers = @($Answers) } `
        -ExpectedStatus $ExpectedStatus
}

function Get-ById {
    param(
        [object[]]$Items,
        [Guid]$Id,
        [string]$Name
    )

    $item = @($Items) | Where-Object { [Guid]$_.id -eq $Id } | Select-Object -First 1
    if ($null -eq $item) {
        Fail "$Name was not found." $Id $null
    }

    return $item
}

function Get-QuestionByType {
    param(
        [object[]]$Questions,
        [string]$Type
    )

    $question = @($Questions) | Where-Object { $_.type -eq $Type } | Select-Object -First 1
    if ($null -eq $question) {
        Fail "Question type was not found." $Type $null
    }

    return $question
}

function Get-OptionByValue {
    param(
        [object[]]$Options,
        [string]$Value
    )

    $option = @($Options) | Where-Object { $_.value -eq $Value } | Select-Object -First 1
    if ($null -eq $option) {
        Fail "Option value was not found." $Value $null
    }

    return $option
}

function Get-MatrixRowByText {
    param(
        [object[]]$Rows,
        [string]$Text
    )

    $row = @($Rows) | Where-Object { $_.text -eq $Text } | Select-Object -First 1
    if ($null -eq $row) {
        Fail "Matrix row was not found." $Text $null
    }

    return $row
}

function Get-QuestionResult {
    param(
        [object[]]$Results,
        [Guid]$QuestionId
    )

    $result = @($Results) | Where-Object { [Guid]$_.questionId -eq $QuestionId } | Select-Object -First 1
    if ($null -eq $result) {
        Fail "Question result was not found." $QuestionId $null
    }

    return $result
}

function Assert-ChoiceResult {
    param(
        [object]$QuestionResult,
        [string]$Value,
        [int]$ExpectedCount,
        [decimal]$ExpectedPercentage
    )

    $option = @($QuestionResult.choice.options) | Where-Object { $_.value -eq $Value } | Select-Object -First 1
    if ($null -eq $option) {
        Fail "Choice result option was not found." $Value $null
    }

    Assert-Equal $ExpectedCount ([int]$option.count) "Unexpected count for option $Value."
    Assert-DecimalApprox $ExpectedPercentage $option.percentage "Unexpected percentage for option $Value."
}

function Assert-OtherChoiceResult {
    param(
        [object]$QuestionResult,
        [int]$ExpectedCount,
        [decimal]$ExpectedPercentage,
        [string[]]$ExpectedValues
    )

    Assert-True ($null -ne $QuestionResult.choice.other) "Expected other choice results."
    Assert-Equal $ExpectedCount ([int]$QuestionResult.choice.other.count) "Unexpected other choice count."
    Assert-DecimalApprox $ExpectedPercentage $QuestionResult.choice.other.percentage "Unexpected other choice percentage."

    foreach ($value in $ExpectedValues) {
        Assert-ContainsValue @($QuestionResult.choice.other.values) $value "Other choice value missing."
    }
}

function Assert-MatrixResult {
    param(
        [object]$QuestionResult,
        [Guid]$RowId,
        [string]$OptionValue,
        [int]$ExpectedCount,
        [decimal]$ExpectedPercentage
    )

    $row = @($QuestionResult.matrix.rows) | Where-Object { [Guid]$_.rowId -eq $RowId } | Select-Object -First 1
    if ($null -eq $row) {
        Fail "Matrix result row was not found." $RowId $null
    }

    $option = @($row.options) | Where-Object { $_.value -eq $OptionValue } | Select-Object -First 1
    if ($null -eq $option) {
        Fail "Matrix result option was not found." $OptionValue $null
    }

    Assert-Equal $ExpectedCount ([int]$option.count) "Unexpected matrix count for $($row.rowText) / $OptionValue."
    Assert-DecimalApprox $ExpectedPercentage $option.percentage "Unexpected matrix percentage for $($row.rowText) / $OptionValue."
}

function New-ValidAnswers {
    param(
        [string]$SingleValue = $null,
        [string]$SingleOtherText = $null,
        [string[]]$MultipleValues,
        [string]$MultipleOtherText = $null,
        [string]$ShortText,
        [string]$LongText,
        [string]$LongTextComment = $null,
        [int]$Rating,
        [string]$ClaridadValue,
        [string]$OrganizacionValue
    )

    $singleOptionIds = @()
    if (-not [string]::IsNullOrWhiteSpace($SingleValue)) {
        $singleOption = Get-OptionByValue @($script:SingleQuestion.options) $SingleValue
        $singleOptionIds = @($singleOption.id)
    }

    $multipleOptions = @()
    foreach ($value in $MultipleValues) {
        $multipleOptions += Get-OptionByValue @($script:MultipleQuestion.options) $value
    }

    $claridadOption = Get-OptionByValue @($script:MatrixQuestion.options) $ClaridadValue
    $organizacionOption = Get-OptionByValue @($script:MatrixQuestion.options) $OrganizacionValue

    return @(
        (New-Answer -QuestionId $script:SingleQuestion.id -OptionIds $singleOptionIds -OtherText $SingleOtherText),
        (New-Answer -QuestionId $script:MultipleQuestion.id -OptionIds @($multipleOptions | ForEach-Object { $_.id }) -OtherText $MultipleOtherText),
        (New-Answer -QuestionId $script:ShortTextQuestion.id -TextValue $ShortText),
        (New-Answer -QuestionId $script:LongTextQuestion.id -TextValue $LongText -Comment $LongTextComment),
        (New-Answer -QuestionId $script:RatingQuestion.id -NumericValue $Rating),
        (New-Answer -QuestionId $script:MatrixQuestion.id -MatrixAnswers @(
            @{ rowId = $script:ClaridadRow.id; optionId = $claridadOption.id },
            @{ rowId = $script:OrganizacionRow.id; optionId = $organizacionOption.id }
        ))
    )
}

Write-Host "AcademicSurveySystem - E2E survey question types validation" -ForegroundColor Green
Write-Host "BaseUrl: $script:BaseUrl"
Write-Host "AdminEmail: $AdminEmail"

$securePassword = Read-Host "Admin password" -AsSecureString
$adminPassword = Convert-SecureStringToPlainText $securePassword

try {
    Write-Step "Login as administrator"
    $loginResponse = Invoke-Api `
        -Method "POST" `
        -Path "/api/auth/login" `
        -Body @{ email = $AdminEmail; password = $adminPassword } `
        -ExpectedStatus @(200)
}
finally {
    $adminPassword = $null
    Remove-Variable -Name adminPassword -ErrorAction SilentlyContinue
}

$token = $loginResponse.Body.accessToken
Assert-True (-not [string]::IsNullOrWhiteSpace($token)) "Login did not return an access token."

$careerId = [Guid]"03a7170b-0513-4472-8bff-ee6720642e9b"
$subjectId = [Guid]"97ae7c49-4973-4b0a-8dcd-5fd4d03f6ac4"
$academicCycleId = [Guid]"3a83075d-af4b-45c0-80e0-fd7fa439034f"
$teacherSubjectAssignmentId = [Guid]"3cf7876e-97a8-441d-8e0a-ce32f61cf6d3"

Write-Step "Verify academic catalog resources"
$career = (Invoke-Api -Method "GET" -Path "/api/academic/careers/$careerId" -Token $token -ExpectedStatus @(200)).Body
$subject = (Invoke-Api -Method "GET" -Path "/api/academic/subjects/$subjectId" -Token $token -ExpectedStatus @(200)).Body
$academicCycle = (Invoke-Api -Method "GET" -Path "/api/academic/academic-cycles/$academicCycleId" -Token $token -ExpectedStatus @(200)).Body
$teacherSubjectAssignment = (Invoke-Api -Method "GET" -Path "/api/academic/teacher-subject-assignments/$teacherSubjectAssignmentId" -Token $token -ExpectedStatus @(200)).Body

Assert-Equal $careerId ([Guid]$career.id) "Career id mismatch."
Assert-Equal $subjectId ([Guid]$subject.id) "Subject id mismatch."
Assert-Equal $academicCycleId ([Guid]$academicCycle.id) "Academic cycle id mismatch."
Assert-Equal $teacherSubjectAssignmentId ([Guid]$teacherSubjectAssignment.id) "Teacher subject assignment id mismatch."
Assert-ActiveResource $career "Career"
Assert-ActiveResource $subject "Subject"
Assert-ActiveResource $academicCycle "AcademicCycle"
Assert-ActiveResource $teacherSubjectAssignment "TeacherSubjectAssignment"

Write-Step "Create survey template"
$surveyDetail = (Invoke-Api `
    -Method "POST" `
    -Path "/api/surveys" `
    -Token $token `
    -Body @{
        title = "VALIDACION E2E - Tipos de pregunta"
        description = "Plantilla creada por script de validacion E2E."
        target = "Student"
        isAnonymous = $true
    } `
    -ExpectedStatus @(201)).Body
$surveyId = [Guid]$surveyDetail.id

Write-Step "Create section"
$surveyDetail = (Invoke-Api `
    -Method "POST" `
    -Path "/api/surveys/$surveyId/sections" `
    -Token $token `
    -Body @{
        title = "Validacion de tipos"
        description = "Seccion para validar todos los tipos de pregunta."
        order = 1
    } `
    -ExpectedStatus @(201)).Body
$section = @($surveyDetail.sections) | Select-Object -First 1
Assert-True ($null -ne $section) "Survey section was not returned."
$sectionId = [Guid]$section.id

Write-Step "Create questions"
$questionDefinitions = @(
    @{ text = "Seleccione una opcion"; type = "SingleChoice"; isRequired = $true; allowsComment = $false; allowsOtherOption = $true; order = 1 },
    @{ text = "Seleccione una o mas opciones"; type = "MultipleChoice"; isRequired = $true; allowsComment = $false; allowsOtherOption = $true; order = 2 },
    @{ text = "Respuesta corta"; type = "ShortText"; isRequired = $true; allowsComment = $false; allowsOtherOption = $false; order = 3 },
    @{ text = "Respuesta larga"; type = "LongText"; isRequired = $true; allowsComment = $true; allowsOtherOption = $false; order = 4 },
    @{ text = "Califique del 1 al 5"; type = "RatingScale"; isRequired = $true; allowsComment = $false; allowsOtherOption = $false; order = 5; ratingMin = 1; ratingMax = 5 },
    @{ text = "Evalue los siguientes aspectos"; type = "MatrixSingleChoice"; isRequired = $true; allowsComment = $false; allowsOtherOption = $false; order = 6 }
)

foreach ($definition in $questionDefinitions) {
    $surveyDetail = (Invoke-Api `
        -Method "POST" `
        -Path "/api/surveys/$surveyId/sections/$sectionId/questions" `
        -Token $token `
        -Body $definition `
        -ExpectedStatus @(201)).Body
}

$section = Get-ById @($surveyDetail.sections) $sectionId "Section"
$questions = @($section.questions)
Assert-Equal 6 $questions.Count "Survey must contain six questions."

$script:SingleQuestion = Get-QuestionByType $questions "SingleChoice"
$script:MultipleQuestion = Get-QuestionByType $questions "MultipleChoice"
$script:ShortTextQuestion = Get-QuestionByType $questions "ShortText"
$script:LongTextQuestion = Get-QuestionByType $questions "LongText"
$script:RatingQuestion = Get-QuestionByType $questions "RatingScale"
$script:MatrixQuestion = Get-QuestionByType $questions "MatrixSingleChoice"

Write-Step "Create choice options"
$choiceOptionDefinitions = @(
    @{ question = $script:SingleQuestion; options = @(
        @{ text = "Opcion A"; value = "opcion_a"; order = 1 },
        @{ text = "Opcion B"; value = "opcion_b"; order = 2 },
        @{ text = "Opcion C"; value = "opcion_c"; order = 3 }
    ) },
    @{ question = $script:MultipleQuestion; options = @(
        @{ text = "Multiple A"; value = "multiple_a"; order = 1 },
        @{ text = "Multiple B"; value = "multiple_b"; order = 2 },
        @{ text = "Multiple C"; value = "multiple_c"; order = 3 }
    ) },
    @{ question = $script:MatrixQuestion; options = @(
        @{ text = "Malo"; value = "malo"; order = 1 },
        @{ text = "Bueno"; value = "bueno"; order = 2 },
        @{ text = "Muy bueno"; value = "muy_bueno"; order = 3 }
    ) }
)

foreach ($group in $choiceOptionDefinitions) {
    foreach ($option in $group.options) {
        $surveyDetail = (Invoke-Api `
            -Method "POST" `
            -Path "/api/surveys/$surveyId/sections/$sectionId/questions/$($group.question.id)/options" `
            -Token $token `
            -Body $option `
            -ExpectedStatus @(201)).Body
    }
}

Write-Step "Create matrix rows"
foreach ($row in @(
    @{ text = "Claridad"; order = 1 },
    @{ text = "Organizacion"; order = 2 }
)) {
    $surveyDetail = (Invoke-Api `
        -Method "POST" `
        -Path "/api/surveys/$surveyId/sections/$sectionId/questions/$($script:MatrixQuestion.id)/matrix-rows" `
        -Token $token `
        -Body $row `
        -ExpectedStatus @(201)).Body
}

Write-Step "Verify survey detail before publication"
$surveyDetail = (Invoke-Api -Method "GET" -Path "/api/surveys/$surveyId" -Token $token -ExpectedStatus @(200)).Body
$section = Get-ById @($surveyDetail.sections) $sectionId "Section"
$questions = @($section.questions)
Assert-Equal 6 $questions.Count "Survey detail must contain six questions."

$script:SingleQuestion = Get-QuestionByType $questions "SingleChoice"
$script:MultipleQuestion = Get-QuestionByType $questions "MultipleChoice"
$script:ShortTextQuestion = Get-QuestionByType $questions "ShortText"
$script:LongTextQuestion = Get-QuestionByType $questions "LongText"
$script:RatingQuestion = Get-QuestionByType $questions "RatingScale"
$script:MatrixQuestion = Get-QuestionByType $questions "MatrixSingleChoice"

Assert-Equal 3 @($script:SingleQuestion.options).Count "SingleChoice must have three options."
Assert-Equal 3 @($script:MultipleQuestion.options).Count "MultipleChoice must have three options."
Assert-Equal 3 @($script:MatrixQuestion.options).Count "MatrixSingleChoice must have three options."
Assert-Equal 2 @($script:MatrixQuestion.matrixRows).Count "MatrixSingleChoice must have two rows."

$script:ClaridadRow = Get-MatrixRowByText @($script:MatrixQuestion.matrixRows) "Claridad"
$script:OrganizacionRow = Get-MatrixRowByText @($script:MatrixQuestion.matrixRows) "Organizacion"

Write-Step "Publish survey"
Invoke-Api -Method "PATCH" -Path "/api/surveys/$surveyId/publish" -Token $token -ExpectedStatus @(204) | Out-Null

Write-Step "Create survey assignment"
$assignment = (Invoke-Api `
    -Method "POST" `
    -Path "/api/survey-assignments" `
    -Token $token `
    -Body @{
        surveyId = $surveyId
        careerId = $careerId
        subjectId = $subjectId
        academicCycleId = $academicCycleId
        teacherSubjectAssignmentId = $teacherSubjectAssignmentId
    } `
    -ExpectedStatus @(201)).Body
$assignmentId = [Guid]$assignment.id

Write-Step "Create and open survey session"
$expiresAtUtc = [DateTimeOffset]::UtcNow.AddHours(2).ToString("o")
$session = (Invoke-Api `
    -Method "POST" `
    -Path "/api/survey-sessions" `
    -Token $token `
    -Body @{
        surveyAssignmentId = $assignmentId
        title = "VALIDACION E2E - Tipos de pregunta"
        location = "Script de validacion"
        expiresAtUtc = $expiresAtUtc
    } `
    -ExpectedStatus @(201)).Body
$sessionId = [Guid]$session.id

$openResponse = Invoke-Api -Method "PATCH" -Path "/api/survey-sessions/$sessionId/open" -Token $token -ExpectedStatus @(204)
Assert-Equal 204 ([int]$openResponse.StatusCode) "Open session command must return 204 No Content."

$session = (Invoke-Api -Method "GET" -Path "/api/survey-sessions/$sessionId" -Token $token -ExpectedStatus @(200)).Body
Assert-Equal "Open" $session.status "Survey session status after open must be Open."
$accessCode = $session.accessCode
Assert-True (-not [string]::IsNullOrWhiteSpace($accessCode)) "Opened session did not expose an access code."

Write-Step "Fetch public survey without JWT"
$publicSurvey = (Invoke-Api -Method "GET" -Path "/api/public/survey-sessions/$accessCode" -ExpectedStatus @(200)).Body
Assert-Equal $surveyId ([Guid]$publicSurvey.surveyId) "Public survey id mismatch."
$publicSection = @($publicSurvey.sections) | Select-Object -First 1
Assert-True ($null -ne $publicSection) "Public survey did not return the section."
Assert-Equal 6 @($publicSection.questions).Count "Public survey must expose six questions."
$publicRatingQuestion = Get-QuestionByType @($publicSection.questions) "RatingScale"
Assert-Equal 1 ([int]$publicRatingQuestion.ratingMin) "Public RatingScale ratingMin mismatch."
Assert-Equal 5 ([int]$publicRatingQuestion.ratingMax) "Public RatingScale ratingMax mismatch."
$publicSingleQuestion = Get-QuestionByType @($publicSection.questions) "SingleChoice"
$publicMultipleQuestion = Get-QuestionByType @($publicSection.questions) "MultipleChoice"
$publicLongTextQuestion = Get-QuestionByType @($publicSection.questions) "LongText"
Assert-Equal $true ([bool]$publicSingleQuestion.allowsOtherOption) "Public SingleChoice allowsOtherOption mismatch."
Assert-Equal $true ([bool]$publicMultipleQuestion.allowsOtherOption) "Public MultipleChoice allowsOtherOption mismatch."
Assert-Equal $true ([bool]$publicLongTextQuestion.allowsComment) "Public LongText allowsComment mismatch."

Write-Step "Submit three anonymous responses"
$answers1 = New-ValidAnswers `
    -SingleValue "opcion_a" `
    -MultipleValues @("multiple_a", "multiple_b") `
    -ShortText "Texto corto uno" `
    -LongText "Respuesta larga numero uno" `
    -LongTextComment "Comentario uno" `
    -Rating 5 `
    -ClaridadValue "muy_bueno" `
    -OrganizacionValue "bueno"
$response1 = Submit-Response -AccessCode $accessCode -Answers $answers1

$answers2 = New-ValidAnswers `
    -SingleOtherText " Otra opcion single " `
    -MultipleValues @("multiple_b") `
    -MultipleOtherText "Otra opcion multiple" `
    -ShortText "Texto corto dos" `
    -LongText "Respuesta larga numero dos" `
    -LongTextComment $null `
    -Rating 4 `
    -ClaridadValue "bueno" `
    -OrganizacionValue "bueno"
$response2 = Submit-Response -AccessCode $accessCode -Answers $answers2

$answers3 = New-ValidAnswers `
    -SingleValue "opcion_b" `
    -MultipleValues @("multiple_a", "multiple_c") `
    -ShortText "Texto corto tres" `
    -LongText "Respuesta larga numero tres" `
    -LongTextComment "Comentario tres" `
    -Rating 3 `
    -ClaridadValue "malo" `
    -OrganizacionValue "muy_bueno"
$response3 = Submit-Response -AccessCode $accessCode -Answers $answers3

Write-Step "Validate results summary"
$summary = (Invoke-Api -Method "GET" -Path "/api/results/survey-assignments/$assignmentId/summary" -Token $token -ExpectedStatus @(200)).Body
Assert-Equal 3 ([int]$summary.totalResponses) "Summary totalResponses must be 3."

Write-Step "Validate question results"
$questionResults = @((Invoke-Api -Method "GET" -Path "/api/results/survey-assignments/$assignmentId/questions" -Token $token -ExpectedStatus @(200)).Body)
Assert-Equal 6 $questionResults.Count "Question results must contain six questions."

$singleResult = Get-QuestionResult $questionResults $script:SingleQuestion.id
Assert-Equal 3 ([int]$singleResult.responseCount) "SingleChoice response count mismatch."
Assert-ChoiceResult $singleResult "opcion_a" 1 33.33
Assert-ChoiceResult $singleResult "opcion_b" 1 33.33
Assert-ChoiceResult $singleResult "opcion_c" 0 0
Assert-OtherChoiceResult $singleResult 1 33.33 @("Otra opcion single")

$multipleResult = Get-QuestionResult $questionResults $script:MultipleQuestion.id
Assert-Equal 3 ([int]$multipleResult.responseCount) "MultipleChoice respondent count mismatch."
Assert-ChoiceResult $multipleResult "multiple_a" 2 66.67
Assert-ChoiceResult $multipleResult "multiple_b" 2 66.67
Assert-ChoiceResult $multipleResult "multiple_c" 1 33.33
Assert-OtherChoiceResult $multipleResult 1 33.33 @("Otra opcion multiple")

$shortTextResult = Get-QuestionResult $questionResults $script:ShortTextQuestion.id
Assert-Equal 3 ([int]$shortTextResult.textValues.responseCount) "ShortText response count mismatch."
Assert-ContainsValue @($shortTextResult.textValues.values) "Texto corto uno" "ShortText value missing."
Assert-ContainsValue @($shortTextResult.textValues.values) "Texto corto dos" "ShortText value missing."
Assert-ContainsValue @($shortTextResult.textValues.values) "Texto corto tres" "ShortText value missing."

$longTextResult = Get-QuestionResult $questionResults $script:LongTextQuestion.id
Assert-Equal 3 ([int]$longTextResult.textValues.responseCount) "LongText response count mismatch."
Assert-Equal 3 @($longTextResult.textValues.values).Count "LongText values count mismatch."
Assert-ContainsValue @($longTextResult.textValues.values) "Respuesta larga numero uno" "LongText value missing."
Assert-ContainsValue @($longTextResult.textValues.values) "Respuesta larga numero dos" "LongText value missing."
Assert-ContainsValue @($longTextResult.textValues.values) "Respuesta larga numero tres" "LongText value missing."
Assert-Equal 2 @($longTextResult.comments).Count "LongText comments count mismatch."
Assert-ContainsValue @($longTextResult.comments | ForEach-Object { $_.comment }) "Comentario uno" "LongText comment missing."
Assert-ContainsValue @($longTextResult.comments | ForEach-Object { $_.comment }) "Comentario tres" "LongText comment missing."
Assert-NotContainsValue @($longTextResult.textValues.values) "Comentario uno" "LongText textValues must not contain comments."
Assert-NotContainsValue @($longTextResult.textValues.values) "Comentario tres" "LongText textValues must not contain comments."
Assert-NotContainsValue @($longTextResult.comments | ForEach-Object { $_.comment }) "Respuesta larga numero uno" "LongText comments must not contain text values."
Assert-NotContainsValue @($longTextResult.comments | ForEach-Object { $_.comment }) "Respuesta larga numero dos" "LongText comments must not contain text values."
Assert-NotContainsValue @($longTextResult.comments | ForEach-Object { $_.comment }) "Respuesta larga numero tres" "LongText comments must not contain text values."

$ratingResult = Get-QuestionResult $questionResults $script:RatingQuestion.id
Assert-Equal 3 ([int]$ratingResult.rating.responseCount) "RatingScale response count mismatch."
Assert-Equal 1 ([int]$ratingResult.rating.configuredMinimum) "RatingScale configured minimum mismatch."
Assert-Equal 5 ([int]$ratingResult.rating.configuredMaximum) "RatingScale configured maximum mismatch."
Assert-DecimalApprox 4 $ratingResult.rating.average "RatingScale average mismatch."
Assert-Equal 3 ([int]$ratingResult.rating.minimumObserved) "RatingScale minimum mismatch."
Assert-Equal 5 ([int]$ratingResult.rating.maximumObserved) "RatingScale maximum mismatch."
foreach ($value in @(1, 2)) {
    $distribution = @($ratingResult.rating.distribution) | Where-Object { [int]$_.value -eq $value } | Select-Object -First 1
    Assert-True ($null -ne $distribution) "RatingScale distribution value $value missing."
    Assert-Equal 0 ([int]$distribution.count) "RatingScale distribution count mismatch for $value."
    Assert-DecimalApprox 0 $distribution.percentage "RatingScale distribution percentage mismatch for $value."
}
foreach ($value in @(3, 4, 5)) {
    $distribution = @($ratingResult.rating.distribution) | Where-Object { [int]$_.value -eq $value } | Select-Object -First 1
    Assert-True ($null -ne $distribution) "RatingScale distribution value $value missing."
    Assert-Equal 1 ([int]$distribution.count) "RatingScale distribution count mismatch for $value."
    Assert-DecimalApprox 33.33 $distribution.percentage "RatingScale distribution percentage mismatch for $value."
}

$matrixResult = Get-QuestionResult $questionResults $script:MatrixQuestion.id
Assert-Equal 3 ([int]$matrixResult.responseCount) "MatrixSingleChoice response count mismatch."
Assert-MatrixResult $matrixResult $script:ClaridadRow.id "muy_bueno" 1 33.33
Assert-MatrixResult $matrixResult $script:ClaridadRow.id "bueno" 1 33.33
Assert-MatrixResult $matrixResult $script:ClaridadRow.id "malo" 1 33.33
Assert-MatrixResult $matrixResult $script:OrganizacionRow.id "bueno" 2 66.67
Assert-MatrixResult $matrixResult $script:OrganizacionRow.id "muy_bueno" 1 33.33
Assert-MatrixResult $matrixResult $script:OrganizacionRow.id "malo" 0 0

Write-Step "Run negative validations that must not persist"
$invalidSingle = New-ValidAnswers `
    -SingleValue "opcion_a" `
    -MultipleValues @("multiple_a") `
    -ShortText "Texto negativo" `
    -LongText "Respuesta negativa" `
    -Rating 4 `
    -ClaridadValue "bueno" `
    -OrganizacionValue "bueno"
$singleA = Get-OptionByValue @($script:SingleQuestion.options) "opcion_a"
$singleB = Get-OptionByValue @($script:SingleQuestion.options) "opcion_b"
$invalidSingle[0].optionIds = @($singleA.id, $singleB.id)
Submit-Response -AccessCode $accessCode -Answers $invalidSingle -ExpectedStatus @(400) | Out-Null

$invalidSingle = New-ValidAnswers `
    -SingleValue "opcion_a" `
    -SingleOtherText "Otra opcion single invalida" `
    -MultipleValues @("multiple_a") `
    -ShortText "Texto negativo" `
    -LongText "Respuesta negativa" `
    -Rating 4 `
    -ClaridadValue "bueno" `
    -OrganizacionValue "bueno"
Submit-Response -AccessCode $accessCode -Answers $invalidSingle -ExpectedStatus @(400) | Out-Null

$invalidSingle = New-ValidAnswers `
    -SingleOtherText "   " `
    -MultipleValues @("multiple_a") `
    -ShortText "Texto negativo" `
    -LongText "Respuesta negativa" `
    -Rating 4 `
    -ClaridadValue "bueno" `
    -OrganizacionValue "bueno"
Submit-Response -AccessCode $accessCode -Answers $invalidSingle -ExpectedStatus @(400) | Out-Null

$missingShort = New-ValidAnswers `
    -SingleValue "opcion_a" `
    -MultipleValues @("multiple_a") `
    -ShortText "Texto que sera removido" `
    -LongText "Respuesta negativa" `
    -Rating 4 `
    -ClaridadValue "bueno" `
    -OrganizacionValue "bueno"
$missingShort[2].textValue = $null
Submit-Response -AccessCode $accessCode -Answers $missingShort -ExpectedStatus @(400) | Out-Null

$invalidShortOther = New-ValidAnswers `
    -SingleValue "opcion_a" `
    -MultipleValues @("multiple_a") `
    -ShortText "Texto negativo" `
    -LongText "Respuesta negativa" `
    -Rating 4 `
    -ClaridadValue "bueno" `
    -OrganizacionValue "bueno"
$invalidShortOther[2].otherText = "Otra opcion no permitida"
Submit-Response -AccessCode $accessCode -Answers $invalidShortOther -ExpectedStatus @(400) | Out-Null

$invalidMultiple = New-ValidAnswers `
    -SingleValue "opcion_a" `
    -MultipleValues @("multiple_a") `
    -ShortText "Texto negativo" `
    -LongText "Respuesta negativa" `
    -Rating 4 `
    -ClaridadValue "bueno" `
    -OrganizacionValue "bueno"
$invalidMultiple[1].optionIds = @([Guid]::NewGuid())
Submit-Response -AccessCode $accessCode -Answers $invalidMultiple -ExpectedStatus @(400) | Out-Null

$invalidRating = New-ValidAnswers `
    -SingleValue "opcion_a" `
    -MultipleValues @("multiple_a") `
    -ShortText "Texto negativo" `
    -LongText "Respuesta negativa" `
    -Rating 0 `
    -ClaridadValue "bueno" `
    -OrganizacionValue "bueno"
Submit-Response -AccessCode $accessCode -Answers $invalidRating -ExpectedStatus @(400) | Out-Null

$invalidRating = New-ValidAnswers `
    -SingleValue "opcion_a" `
    -MultipleValues @("multiple_a") `
    -ShortText "Texto negativo" `
    -LongText "Respuesta negativa" `
    -Rating 6 `
    -ClaridadValue "bueno" `
    -OrganizacionValue "bueno"
Submit-Response -AccessCode $accessCode -Answers $invalidRating -ExpectedStatus @(400) | Out-Null

$invalidMatrixRow = New-ValidAnswers `
    -SingleValue "opcion_a" `
    -MultipleValues @("multiple_a") `
    -ShortText "Texto negativo" `
    -LongText "Respuesta negativa" `
    -Rating 4 `
    -ClaridadValue "bueno" `
    -OrganizacionValue "bueno"
$matrixBueno = Get-OptionByValue @($script:MatrixQuestion.options) "bueno"
$invalidMatrixRow[5].matrixAnswers = @(
    @{ rowId = [Guid]::NewGuid(); optionId = $matrixBueno.id },
    @{ rowId = $script:OrganizacionRow.id; optionId = $matrixBueno.id }
)
Submit-Response -AccessCode $accessCode -Answers $invalidMatrixRow -ExpectedStatus @(400) | Out-Null

$invalidMatrixOption = New-ValidAnswers `
    -SingleValue "opcion_a" `
    -MultipleValues @("multiple_a") `
    -ShortText "Texto negativo" `
    -LongText "Respuesta negativa" `
    -Rating 4 `
    -ClaridadValue "bueno" `
    -OrganizacionValue "bueno"
$invalidMatrixOption[5].matrixAnswers = @(
    @{ rowId = $script:ClaridadRow.id; optionId = [Guid]::NewGuid() },
    @{ rowId = $script:OrganizacionRow.id; optionId = $matrixBueno.id }
)
Submit-Response -AccessCode $accessCode -Answers $invalidMatrixOption -ExpectedStatus @(400) | Out-Null

Write-Step "Close session and verify submissions are rejected"
$closeResponse = Invoke-Api -Method "PATCH" -Path "/api/survey-sessions/$sessionId/close" -Token $token -ExpectedStatus @(204)
Assert-Equal 204 ([int]$closeResponse.StatusCode) "Close session command must return 204 No Content."

$closedSession = (Invoke-Api -Method "GET" -Path "/api/survey-sessions/$sessionId" -Token $token -ExpectedStatus @(200)).Body
Assert-Equal "Closed" $closedSession.status "Survey session status after close must be Closed."

$closedResponse = Submit-Response -AccessCode $accessCode -Answers $answers1 -ExpectedStatus @(400)
Assert-True ($closedResponse.RawBody -like "*SurveySession.NotAvailable*") "Closed session response must include SurveySession.NotAvailable."

Write-Step "Verify invalid submissions did not change totals"
$summaryAfterNegatives = (Invoke-Api -Method "GET" -Path "/api/results/survey-assignments/$assignmentId/summary" -Token $token -ExpectedStatus @(200)).Body
Assert-Equal 3 ([int]$summaryAfterNegatives.totalResponses) "Total responses changed after invalid submissions."

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "E2E QUESTION TYPES VALIDATION: PASSED" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Created identifiers:"
Write-Host "SurveyId: $surveyId"
Write-Host "SurveyAssignmentId: $assignmentId"
Write-Host "SurveySessionId: $sessionId"
Write-Host "AccessCode: $accessCode"
Write-Host "ResponseIds: $($response1.Body.responseId), $($response2.Body.responseId), $($response3.Body.responseId)"
Write-Host "QuestionIds:"
Write-Host "  SingleChoice: $($script:SingleQuestion.id)"
Write-Host "  MultipleChoice: $($script:MultipleQuestion.id)"
Write-Host "  ShortText: $($script:ShortTextQuestion.id)"
Write-Host "  LongText: $($script:LongTextQuestion.id)"
Write-Host "  RatingScale: $($script:RatingQuestion.id)"
Write-Host "  MatrixSingleChoice: $($script:MatrixQuestion.id)"
Write-Host ""
Write-Host "Current behavior notes:"
Write-Host "- RatingScale exposes ratingMin/ratingMax in template and public DTOs; this script validates the 1..5 range."
Write-Host "- allowsOtherOption is exposed on choice questions; submissions use otherText and results expose choice.other."
