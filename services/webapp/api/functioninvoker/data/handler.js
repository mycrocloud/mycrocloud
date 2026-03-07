function handler(request) {
    console.log('handling request...')
    return {
        statusCode: 200,
        headers: {
            "content-type": "application/json"
        },
        body: JSON.stringify({ message: `Hello, ${request.body.name}`})
    }
}