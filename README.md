# Blackbird.io Drupal

Blackbird is the new automation backbone for the language technology industry. Blackbird provides enterprise-scale automation and orchestration with a simple no-code/low-code platform. Blackbird enables ambitious organizations to identify, vet and automate as many processes as possible. Not just localization workflows, but any business and IT process. This repository represents an application that is deployable on Blackbird and usable inside the workflow editor.

## Introduction

<!-- begin docs -->

**Drupal** is a free, open-source content management system (CMS) with a large, supportive community. It’s used by millions of people and organizations around the globe to build and maintain their websites.

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
- **Get XLIFF from job** - Get XLIFF file from the job with specified job ID.
- **Update job with XLIFF** - Update job with XLIFF file.

## Events

- **On translation jobs requested** - Polling based event. Returns translation jobs that were requested after the last polling time. 

## Error handling

In case of an error, the app will return an error message. The error message will contain the error code and a message that describes the error. If you encounter an error, please refer to the error message and status code to understand the issue. If you need further assistance, please contact our support team.

Example error message: `Status code: 404, Error: Page not found | Blackbird Demo`

## Feedback

Do you want to use this app or do you have feedback on our implementation? Reach out to us using the [established channels](https://www.blackbird.io/) or create an issue.

<!-- end docs -->
