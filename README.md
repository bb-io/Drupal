# Blackbird.io Drupal

Blackbird is the new automation backbone for the language technology industry. Blackbird provides enterprise-scale automation and orchestration with a simple no-code/low-code platform. Blackbird enables ambitious organizations to identify, vet and automate as many processes as possible. Not just localization workflows, but any business and IT process. This repository represents an application that is deployable on Blackbird and usable inside the workflow editor.

## Introduction

<!-- begin docs -->

**Drupal** is a free, open-source content management system (CMS) with a large, supportive community. It’s used by millions of people and organizations around the globe to build and maintain their websites.

## [**TMGMT Blackbird**](https://git.drupalcode.org/project/tmgmt_blackbird) module configuration

The **TMGMT Blackbird** module provides integration with the Blackbird platform. It allows you to send and receive translation jobs to and from the Blackbird platform. You can find more information about the plugin on the [Drupal TMGMT Blackbird plugin page](https://git.drupalcode.org/project/tmgmt_blackbird).

### Requirements

The module depends on the [Translation Management (TMGMT) module](https://www.drupal.org/project/tmgmt).

### Installation

Install the module as you would normally install a contributed Drupal module. Visit [Installing contributed modules](https://www.drupal.org/docs/8/extending-drupal-8/installing-contributed-modules) for further information.

### Configuration

Go to `/admin/tmgmt/translators` and configure the `Blackbird` plugin. On the configuration form, you will need to provide the following information:

- API Key: The API key used by Blackbird to process translations jobs. The API key needs to be copied to the Blackbird platform settings to complete the integration.
- To prevent having to manually accept all translations, you can enable the `Auto accept finished translations` setting. This will automatically accept all translations when they are completed.

### Translation jobs

When creating a new translation job, you can select the `Blackbird` translator to process the job. Blackbird will then automatically process the job and post the translations back when they are done. The module will expose the following endpoints for `Blackbird` to interact with the translation jobs:

## Before setting up

Before you can connect you need to make sure that:

- You have a Drupal instance
- You have a Drupal API key.

## Connecting

1. Navigate to Apps, and identify the **Drupal** app. You can use search to find it.
2. Click _Add Connection_.
3. Name your connection for future reference e.g. 'My Drupal connection'.
4. Fill in the `Base URL` field. For example: `https://your-drupal-site.com`.
5. Fill in the `API key` you got from Drupal.
6. Click **Connect**.
7. Make sure that connection was added successfully.

![connection](./image/README/connecting.png)

## Actions

- **Search jobs** - Get jobs by specified search parameters.
- **Get job as HTML** - Get HTML file from the job with specified job ID.
- **Update job from HTML** - Update job from HTML file.

## Events

- **On translation jobs requested** - Polling based event. Returns translation jobs that were requested after the last polling time. 

## Error handling

In case of an error, the app will return an error message. The error message will contain the error code and a message that describes the error. If you encounter an error, please refer to the error message and status code to understand the issue. If you need further assistance, please contact our support team.

Example error message: `Status code: 404, Error: Page not found | Blackbird Demo`

## Feedback

Do you want to use this app or do you have feedback on our implementation? Reach out to us using the [established channels](https://www.blackbird.io/) or create an issue.

<!-- end docs -->
