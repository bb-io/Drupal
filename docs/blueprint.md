CMS apps host translatable content and typically render this to end-users on a website. Typical CMS apps contain one or more content units (pages, articles, posts, products, etc.) or this unit differentiation is completely customizable for some CMS apps.

Because of this categorization, we also classify certain apps as CMS that are not traditionally viewed as CMS apps. Think Shopify (e-commerce), Akeneo (PIM), and Braze (email marketing). Ultimately their fundamental action are still the same: content can be created, downloaded, translated, and uploaded again.

### Naming

There are apps that only have one contact type (f.e. Contentful only has *entries*). And there are apps that have many different content types (Hubspot has *Blog posts, Landing pages, Site pages, Emails and forms)*.

If an app has multiple content types, we always unify all actions and refer to it as *content.* In each action there will then be an optional multi-select static dropdown to specify the content types if required. Therefore in Hubspot you will see *Download content, Upload content, On contend created or updated,* .etc. At the same time, if you need to refer to the multiple of content without being able to name it what it is, use the word *Items.* See action *Search content*

If an app only has one specific content type, we always refer to the name of that content. F.e. Contentful: *Download entry, Upload entry, On entry created or updated*. However, in the rest of this page we will just refer to *content*.

Previously we used to name upload/download actions very explicitly (*Download entry as HTML file)*. We do not do this anymore since the interoperability is sufficient enough the user should not have to worry about the format.

We use the terms *Download* and *Upload* to signify content/files being transferred between Blackbird and the app. This decision was made after a lot of user research and feedback 😉.

### Content format & identification

When using the *Download content* action, by default we convert content to HTML for interoperability and CAT context. We give the user the ability to also download the content in its original format (usually some JSON) using the *File format* optional input. For this input use the `DownloadFileFormatHandler` from the Blueprints package. For the *Upload content* action to work properly, it should know what the original content ID was. Therefore, when we convert content to HTML, we also embed the Content ID in an HTML meta attribute for proper roundtripping.

<aside>
💡

For more conventions see:

Interoperability App requirements

</aside>

## Actions

### Search content `BlueprintAction.SearchContent`

**Outputs**  [`IDownloadContentInput`]

- [*Items*] - The item metadata should at least include the *Content ID*

---

### Download content `BlueprintAction.DownloadContent`

**Inputs** `IDownloadContentInput`

- *Content ID**
- *File format* (Static handler: `DownloadFileFormatHandler`)

**Outputs** **`IDownloadContentOutput`

- *Content*

---

### Upload content `BlueprintAction.UploadContent`

**Inputs** `IUploadContentInput`

- *Content**
- *Locale**
- *Content ID*

<aside>
💡

When processing the Content file. Usage of the `Blackbird.Filters` library **is required** here in order to read files that could have been turned into XLIFF by other actions. Example from Contentful:

```csharp
var file = await fileManagementClient.DownloadAsync(input.File);
var transformationResult = Transformation.Load(file, input.Content.Name, input.Content.ContentType);
var contentResult = transformationResult.Target();
if (contentResult.Success)
{
    content = contentResult.Value.ToStream().ReadString();
}
else
{
    InvocationContext.Logger?.LogInformation($"Not a Blackbird interoperable file: {transformationResult.Error}", []);
    content = file.ReadString();
}

// Continue processing HTML content file

// Similar to other Content apps, we need to return the transformation with extra metadata
 if (transformationResult.Success)
 {
	    var transformation = transformationResult.Value;
	    var originalEntry = await GetEntry(new()
      {
         EntryId = input.ContentId ?? mainEntryInfo?.EntryId,
         Environment = input.Environment
      }, new LocaleOptionalIdentifier { Locale = input.Locale });

      var entryId = input.ContentId ?? mainEntryInfo?.EntryId;
      transformation.TargetSystemReference.ContentId = originalEntry.ContentId;
      transformation.TargetSystemReference.ContentName = originalEntry.Title;
      transformation.TargetSystemReference.AdminUrl = client.GetEntryEditorUrl(originalEntry.ContentId);
      transformation.TargetSystemReference.SystemName = "Contentful";
      transformation.TargetSystemReference.SystemRef = "https://www.contentful.com/";
      transformation.TargetLanguage = input.Locale;

			// Bilingual files should return a bilingual with metadata
     if (transformationResult.WasBilingual)
     {
         output.Content = await fileManagementClient.UploadAsync(
             transformation.ToStream(),
             MediaTypes.Xliff2,
             transformation.BilingualFileName);
     }
     else
     {
		     // And monolingual inputs should return the same with the metadata update
         var targetResult = transformation.Target();
         if (!targetResult.Success)
         {
             output.Content = input.Content;
             InvocationContext.Logger?.LogError($"Failed to load target file: {targetResult.Error}", []);
         } else
         {
             var target = targetResult.Value;
             target.SystemReference = transformation.TargetSystemReference; // IMPORTANT!

             output.Content = await fileManagementClient.UploadAsync(
                 target.ToStream(),
                 target.OriginalMediaType,
                 target.OriginalName);
         }
     }
 }
 else
 {
     output.Content = input.Content;
 }
```

</aside>

<aside>
💡

Don’t forget to add as much metadata to the HTML file as possible. See

</aside>

## Events

### On content updated `BlueprintEvent.ContentCreatedOrUpdated`

**Outputs**  `IDownloadContentInput`

---

### On content updated `BlueprintEvent.ContentCreatedOrUpdatedMultiple`

**Outputs**  [`IDownloadContentInput` `IMultiDownloadableContentOutput`]

- [*Items*] - The item metadata should at least include the *Content ID*

<aside>
💡

The Multiple variant is generally used in polling Apps, the single variant in webhook Apps

</aside>