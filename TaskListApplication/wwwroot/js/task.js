$(function() {
    $('#taskModal').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget) // Button that triggered the modal
        var listName = button.data('listname') // Extract info from data-* attributes
        var listId = button.data('listid')
        // If necessary, you could initiate an AJAX request here (and then do the updating in a callback).
        // Update the modal's content. We'll use jQuery here, but you could use a data binding library or other methods instead.
        var modal = $(this)
        modal.find('#owner-list').val(listName)
        modal.find('#owner-id').val(listId)
    });
    
    $('#editTaskModal').on('show.bs.modal', function(event) {
        var button = $(event.relatedTarget);
        var taskId = button.data('taskid')
        var taskListId = button.data('listid')
        var taskTitle = button.data('title')
        var taskNote = button.data('note')
        var modal = $(this)
        modal.find('#task-id').val(taskId)
        modal.find('#task-name').val(taskTitle)
        modal.find('#task-note').val(taskNote)
        modal.find('#' + taskListId).attr('selected', 'selected')
    });
});